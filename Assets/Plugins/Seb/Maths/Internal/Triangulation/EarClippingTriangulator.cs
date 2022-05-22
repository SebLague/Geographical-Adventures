using System.Collections.Generic;
using UnityEngine;

namespace Seb.MathsHelper.Triangulation
{
	/*
     * Handles triangulation of given polygon using the 'ear-clipping' algorithm.
     * The implementation is based on the following paper:
     * https://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf
	 *
	 * WARNING! I recall this having some bugs, so should get around to doing a rewrite at some point...
     */

	public static class Triangulator
	{


		public static int[] Triangulate(Vector2[] polygonPoints)
		{
			return Triangulate(new Polygon(polygonPoints));
		}

		public static int[] Triangulate(Polygon polygon)
		{
			int numHoleToHullConnectionVerts = 2 * polygon.numHoles; // 2 verts are added when connecting a hole to the hull.
			int totalNumVerts = polygon.numPoints + numHoleToHullConnectionVerts;
			int[] tris = new int[(totalNumVerts - 2) * 3];
			LinkedList<Vertex> vertsInClippedPolygon = GenerateVertexList(polygon);
			int triIndex = 0;

			while (vertsInClippedPolygon.Count >= 3)
			{
				bool hasRemovedEarThisIteration = false;
				LinkedListNode<Vertex> vertexNode = vertsInClippedPolygon.First;
				for (int i = 0; i < vertsInClippedPolygon.Count; i++)
				{
					LinkedListNode<Vertex> prevVertexNode = vertexNode.Previous ?? vertsInClippedPolygon.Last;
					LinkedListNode<Vertex> nextVertexNode = vertexNode.Next ?? vertsInClippedPolygon.First;

					if (vertexNode.Value.isConvex)
					{
						if (!TriangleContainsVertex(prevVertexNode.Value, vertexNode.Value, nextVertexNode.Value, vertsInClippedPolygon))
						{
							// check if removal of ear makes prev/next vertex convex (if was previously reflex)
							if (!prevVertexNode.Value.isConvex)
							{
								LinkedListNode<Vertex> prevOfPrev = prevVertexNode.Previous ?? vertsInClippedPolygon.Last;

								prevVertexNode.Value.isConvex = IsConvex(prevOfPrev.Value.position, prevVertexNode.Value.position, nextVertexNode.Value.position);
							}
							if (!nextVertexNode.Value.isConvex)
							{
								LinkedListNode<Vertex> nextOfNext = nextVertexNode.Next ?? vertsInClippedPolygon.First;
								nextVertexNode.Value.isConvex = IsConvex(prevVertexNode.Value.position, nextVertexNode.Value.position, nextOfNext.Value.position);
							}

							// add triangle to tri array
							tris[triIndex * 3 + 2] = prevVertexNode.Value.index;
							tris[triIndex * 3 + 1] = vertexNode.Value.index;
							tris[triIndex * 3] = nextVertexNode.Value.index;
							triIndex++;

							hasRemovedEarThisIteration = true;
							vertsInClippedPolygon.Remove(vertexNode);
							break;
						}
					}


					vertexNode = nextVertexNode;
				}

				if (!hasRemovedEarThisIteration)
				{
					Debug.LogError("Error triangulating mesh. Aborted.");
					return null;
				}
			}
			return tris;
		}

		// Creates a linked list of all vertices in the polygon, with the hole vertices joined to the hull at optimal points.
		static LinkedList<Vertex> GenerateVertexList(Polygon polygon)
		{
			LinkedList<Vertex> vertexList = new LinkedList<Vertex>();
			LinkedListNode<Vertex> currentNode = null;

			// Add all hull points to the linked list
			for (int i = 0; i < polygon.numHullPoints; i++)
			{
				int prevPointIndex = (i - 1 + polygon.numHullPoints) % polygon.numHullPoints;
				int nextPointIndex = (i + 1) % polygon.numHullPoints;

				bool vertexIsConvex = IsConvex(polygon.points[prevPointIndex], polygon.points[i], polygon.points[nextPointIndex]);
				Vertex currentHullVertex = new Vertex(polygon.points[i], i, vertexIsConvex);

				if (currentNode == null)
					currentNode = vertexList.AddFirst(currentHullVertex);
				else
					currentNode = vertexList.AddAfter(currentNode, currentHullVertex);
			}

			// Process holes:
			List<HoleData> sortedHoleData = new List<HoleData>();

			for (int holeIndex = 0; holeIndex < polygon.numHoles; holeIndex++)
			{
				// Find index of rightmost point in hole. This 'bridge' point is where the hole will be connected to the hull.
				Vector2 holeBridgePoint = new Vector2(float.MinValue, 0);
				int holeBridgeIndex = 0;
				for (int i = 0; i < polygon.numPointsPerHole[holeIndex]; i++)
				{
					if (polygon.GetHolePoint(i, holeIndex).x > holeBridgePoint.x)
					{
						holeBridgePoint = polygon.GetHolePoint(i, holeIndex);
						holeBridgeIndex = i;

					}
				}
				sortedHoleData.Add(new HoleData(holeIndex, holeBridgeIndex, holeBridgePoint));
			}
			// Sort hole data so that holes furthest to the right are first
			sortedHoleData.Sort((x, y) => (x.bridgePoint.x > y.bridgePoint.x) ? -1 : 1);

			foreach (HoleData holeData in sortedHoleData)
			{

				// Find first edge which intersects with rightwards ray originating at the hole bridge point.
				Vector2 rayIntersectPoint = new Vector2(float.MaxValue, holeData.bridgePoint.y);
				List<LinkedListNode<Vertex>> hullNodesPotentiallyInBridgeTriangle = new List<LinkedListNode<Vertex>>();
				LinkedListNode<Vertex> initialBridgeNodeOnHull = null;
				currentNode = vertexList.First;
				while (currentNode != null)
				{
					LinkedListNode<Vertex> nextNode = (currentNode.Next == null) ? vertexList.First : currentNode.Next;
					Vector2 p0 = currentNode.Value.position;
					Vector2 p1 = nextNode.Value.position;

					// at least one point must be to right of holeData.bridgePoint for intersection with ray to be possible
					if (p0.x > holeData.bridgePoint.x || p1.x > holeData.bridgePoint.x)
					{
						// one point is above, one point is below
						if (p0.y > holeData.bridgePoint.y != p1.y > holeData.bridgePoint.y)
						{
							float rayIntersectX = p1.x; // only true if line p0,p1 is vertical
							if (!Mathf.Approximately(p0.x, p1.x))
							{
								float intersectY = holeData.bridgePoint.y;
								float gradient = (p0.y - p1.y) / (p0.x - p1.x);
								float c = p1.y - gradient * p1.x;
								rayIntersectX = (intersectY - c) / gradient;
							}

							// intersection must be to right of bridge point
							if (rayIntersectX > holeData.bridgePoint.x)
							{
								LinkedListNode<Vertex> potentialNewBridgeNode = (p0.x > p1.x) ? currentNode : nextNode;
								// if two intersections occur at same x position this means is duplicate edge
								// duplicate edges occur where a hole has been joined to the outer polygon
								bool isDuplicateEdge = Mathf.Approximately(rayIntersectX, rayIntersectPoint.x);

								// connect to duplicate edge (the one that leads away from the other, already connected hole, and back to the original hull) if the
								// current hole's bridge point is higher up than the bridge point of the other hole (so that the new bridge connection doesn't intersect).
								bool connectToThisDuplicateEdge = holeData.bridgePoint.y > potentialNewBridgeNode.Previous.Value.position.y;

								if (!isDuplicateEdge || connectToThisDuplicateEdge)
								{
									// if this is the closest ray intersection thus far, set bridge hull node to point in line having greater x pos (since def to right of hole).
									if (rayIntersectX < rayIntersectPoint.x || isDuplicateEdge)
									{
										rayIntersectPoint.x = rayIntersectX;
										initialBridgeNodeOnHull = potentialNewBridgeNode;
									}
								}
							}
						}
					}

					// Determine if current node might lie inside the triangle formed by holeBridgePoint, rayIntersection, and bridgeNodeOnHull
					// We only need consider those which are reflex, since only these will be candidates for visibility from holeBridgePoint.
					// A list of these nodes is kept so that in next step it is not necessary to iterate over all nodes again.
					if (currentNode != initialBridgeNodeOnHull)
					{
						if (!currentNode.Value.isConvex && p0.x > holeData.bridgePoint.x)
						{
							hullNodesPotentiallyInBridgeTriangle.Add(currentNode);
						}
					}
					currentNode = currentNode.Next;
				}

				// Check triangle formed by hullBridgePoint, rayIntersection, and bridgeNodeOnHull.
				// If this triangle contains any points, those points compete to become new bridgeNodeOnHull
				LinkedListNode<Vertex> validBridgeNodeOnHull = initialBridgeNodeOnHull;
				foreach (LinkedListNode<Vertex> nodePotentiallyInTriangle in hullNodesPotentiallyInBridgeTriangle)
				{
					if (nodePotentiallyInTriangle.Value.index == initialBridgeNodeOnHull.Value.index)
					{
						continue;
					}
					// if there is a point inside triangle, this invalidates the current bridge node on hull.

					if (Maths.TriangleContainsPoint(holeData.bridgePoint, rayIntersectPoint, initialBridgeNodeOnHull.Value.position, nodePotentiallyInTriangle.Value.position))
					{
						// Duplicate points occur at hole and hull bridge points.
						bool isDuplicatePoint = validBridgeNodeOnHull.Value.position == nodePotentiallyInTriangle.Value.position;

						// if multiple nodes inside triangle, we want to choose the one with smallest angle from holeBridgeNode.
						// if is a duplicate point, then use the one occurring later in the list
						float currentDstFromHoleBridgeY = Mathf.Abs(holeData.bridgePoint.y - validBridgeNodeOnHull.Value.position.y);
						float pointInTriDstFromHoleBridgeY = Mathf.Abs(holeData.bridgePoint.y - nodePotentiallyInTriangle.Value.position.y);

						if (pointInTriDstFromHoleBridgeY < currentDstFromHoleBridgeY || isDuplicatePoint)
						{
							validBridgeNodeOnHull = nodePotentiallyInTriangle;

						}
					}
				}

				// Insert hole points (starting at holeBridgeNode) into vertex list at validBridgeNodeOnHull
				currentNode = validBridgeNodeOnHull;
				for (int i = holeData.bridgeIndex; i <= polygon.numPointsPerHole[holeData.holeIndex] + holeData.bridgeIndex; i++)
				{
					int previousIndex = currentNode.Value.index;
					int currentIndex = polygon.IndexOfPointInHole(i % polygon.numPointsPerHole[holeData.holeIndex], holeData.holeIndex);
					int nextIndex = polygon.IndexOfPointInHole((i + 1) % polygon.numPointsPerHole[holeData.holeIndex], holeData.holeIndex);

					if (i == polygon.numPointsPerHole[holeData.holeIndex] + holeData.bridgeIndex) // have come back to starting point
					{
						nextIndex = validBridgeNodeOnHull.Value.index; // next point is back to the point on the hull
					}

					bool vertexIsConvex = IsConvex(polygon.points[previousIndex], polygon.points[currentIndex], polygon.points[nextIndex]);
					Vertex holeVertex = new Vertex(polygon.points[currentIndex], currentIndex, vertexIsConvex);
					currentNode = vertexList.AddAfter(currentNode, holeVertex);
				}

				// Add duplicate hull bridge vert now that we've come all the way around. Also set its concavity
				Vector2 nextVertexPos = (currentNode.Next == null) ? vertexList.First.Value.position : currentNode.Next.Value.position;
				bool isConvex = IsConvex(holeData.bridgePoint, validBridgeNodeOnHull.Value.position, nextVertexPos);
				Vertex repeatStartHullVert = new Vertex(validBridgeNodeOnHull.Value.position, validBridgeNodeOnHull.Value.index, isConvex);
				vertexList.AddAfter(currentNode, repeatStartHullVert);

				//Set concavity of initial hull bridge vert, since it may have changed now that it leads to hole vert
				LinkedListNode<Vertex> nodeBeforeStartBridgeNodeOnHull = (validBridgeNodeOnHull.Previous == null) ? vertexList.Last : validBridgeNodeOnHull.Previous;
				LinkedListNode<Vertex> nodeAfterStartBridgeNodeOnHull = (validBridgeNodeOnHull.Next == null) ? vertexList.First : validBridgeNodeOnHull.Next;
				validBridgeNodeOnHull.Value.isConvex = IsConvex(nodeBeforeStartBridgeNodeOnHull.Value.position, validBridgeNodeOnHull.Value.position, nodeAfterStartBridgeNodeOnHull.Value.position);
			}
			return vertexList;
		}


		// check if triangle contains any verts (note, only necessary to check reflex verts).
		static bool TriangleContainsVertex(Vertex v0, Vertex v1, Vertex v2, LinkedList<Vertex> vertsInClippedPolygon)
		{
			LinkedListNode<Vertex> vertexNode = vertsInClippedPolygon.First;
			for (int i = 0; i < vertsInClippedPolygon.Count; i++)
			{
				if (!vertexNode.Value.isConvex) // convex verts will never be inside triangle
				{
					Vertex vertexToCheck = vertexNode.Value;
					if (vertexToCheck.index != v0.index && vertexToCheck.index != v1.index && vertexToCheck.index != v2.index) // dont check verts that make up triangle
					{
						if (Maths.TriangleContainsPoint(v0.position, v1.position, v2.position, vertexToCheck.position))
						{
							return true;
						}
					}
				}
				vertexNode = vertexNode.Next;
			}

			return false;
		}


		// v1 is considered a convex vertex if v0-v1-v2 are wound in a counter-clockwise order.
		static bool IsConvex(Vector2 v0, Vector2 v1, Vector2 v2)
		{
			return !Maths.TriangleIsClockwise(v0, v1, v2);
		}

		public struct HoleData
		{
			public readonly int holeIndex;
			public readonly int bridgeIndex;
			public readonly Vector2 bridgePoint;

			public HoleData(int holeIndex, int bridgeIndex, Vector2 bridgePoint)
			{
				this.holeIndex = holeIndex;
				this.bridgeIndex = bridgeIndex;
				this.bridgePoint = bridgePoint;
			}
		}

		public class Vertex
		{
			public readonly Vector2 position;
			public readonly int index;
			public bool isConvex;

			public Vertex(Vector2 position, int index, bool isConvex)
			{
				this.position = position;
				this.index = index;
				this.isConvex = isConvex;
			}
		}
	}

}