using UnityEngine;
using UnityEditor;
using PathCreation;

namespace PathCreationEditor
{
    public static class MouseUtility
    {
        /// <summary>
        /// Determines mouse position in world. If PathSpace is xy/xz, the position will be locked to that plane.
        /// If PathSpace is xyz, will attempt to raycast to a reasonably close object, or return the position
        /// at depthFor3DSpace distance from the current view
        /// </summary>
        public static Vector3 GetMouseWorldPosition(PathSpace space, float depthFor3DSpace = 10)
        {
            var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var worldMouse = Physics.Raycast(mouseRay, out var hitInfo, depthFor3DSpace * 2f) ? 
                hitInfo.point : mouseRay.GetPoint(depthFor3DSpace);

            // Mouse can only move on XY plane
            if (space == PathSpace.xy)
            {
                var zDir = mouseRay.direction.z;
                if (zDir != 0)
                {
                    var dstToXYPlane = Mathf.Abs(mouseRay.origin.z / zDir);
                    worldMouse = mouseRay.GetPoint(dstToXYPlane);
                }
            }
            // Mouse can only move on XZ plane 
            else if (space == PathSpace.xz)
            {
                var yDir = mouseRay.direction.y;
                if (yDir != 0)
                {
                    var dstToXZPlane = Mathf.Abs(mouseRay.origin.y / yDir);
                    worldMouse = mouseRay.GetPoint(dstToXZPlane);
                }
            }

            return worldMouse;
        }

    }
}
