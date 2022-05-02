
namespace TriangleNet.Smoothing
{
    using System;
    using TriangleNet.Topology.DCEL;
    using TriangleNet.Voronoi;

    /// <summary>
    /// Factory which re-uses objects in the smoothing loop to enhance performance.
    /// </summary>
    /// <remarks>
    /// See <see cref="SimpleSmoother"/>.
    /// </remarks>
    class VoronoiFactory : IVoronoiFactory
    {
        ObjectPool<Vertex> vertices;
        ObjectPool<HalfEdge> edges;
        ObjectPool<Face> faces;

        public VoronoiFactory()
        {
            vertices = new ObjectPool<Vertex>();
            edges = new ObjectPool<HalfEdge>();
            faces = new ObjectPool<Face>();
        }

        public void Initialize(int vertexCount, int edgeCount, int faceCount)
        {
            vertices.Capacity = vertexCount;
            edges.Capacity = edgeCount;
            faces.Capacity = faceCount;

            for (int i = vertices.Count; i < vertexCount; i++)
            {
                vertices.Put(new Vertex(0, 0));
            }


            for (int i = edges.Count; i < edgeCount; i++)
            {
                edges.Put(new HalfEdge(null));
            }

            for (int i = faces.Count; i < faceCount; i++)
            {
                faces.Put(new Face(null));
            }

            Reset();
        }

        public void Reset()
        {
            vertices.Release();
            edges.Release();
            faces.Release();
        }

        public Vertex CreateVertex(double x, double y)
        {
            Vertex vertex;

            if (vertices.TryGet(out vertex))
            {
                vertex.x = x;
                vertex.y = y;
                vertex.leaving = null;

                return vertex;
            }

            vertex = new Vertex(x, y);

            vertices.Put(vertex);

            return vertex;
        }

        public HalfEdge CreateHalfEdge(Vertex origin, Face face)
        {
            HalfEdge edge;

            if (edges.TryGet(out edge))
            {
                edge.origin = origin;
                edge.face = face;
                edge.next = null;
                edge.twin = null;

                if (face != null && face.edge == null)
                {
                    face.edge = edge;
                }

                return edge;
            }

            edge = new HalfEdge(origin, face);

            edges.Put(edge);

            return edge;
        }

        public Face CreateFace(Geometry.Vertex vertex)
        {
            Face face;

            if (faces.TryGet(out face))
            {
                face.id = vertex.id;
                face.generator = vertex;
                face.edge = null;

                return face;
            }

            face = new Face(vertex);

            faces.Put(face);

            return face;
        }

        class ObjectPool<T> where T : class
        {
            int index, count;

            T[] pool;

            public int Count
            {
                get { return count; }
            }


            public int Capacity
            {
                get { return this.pool.Length; }
                set { Resize(value); }
            }

            public ObjectPool(int capacity = 3)
            {
                this.index = 0;
                this.count = 0;

                this.pool = new T[capacity];
            }

            public ObjectPool(T[] pool)
            {
                this.index = 0;
                this.count = 0;

                this.pool = pool;
            }

            public bool TryGet(out T obj)
            {
                if (this.index < this.count)
                {
                    obj = this.pool[this.index++];

                    return true;
                }

                obj = null;

                return false;
            }

            public void Put(T obj)
            {
                var capacity = this.pool.Length;

                if (capacity <= this.count)
                {
                    Resize(2 * capacity);
                }

                this.pool[this.count++] = obj;

                this.index++;
            }

            public void Release()
            {
                this.index = 0;
            }

            private void Resize(int size)
            {
                if (size > this.count)
                {
                    Array.Resize(ref this.pool, size);
                }
            }
        }
    }
}
