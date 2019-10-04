
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZombE
{
    public static class LightColliderUtils
    {
        private static List<Vector2> tempPoints = new List<Vector2>();
        public static List<Vector2> GetVertices(this CompositeCollider2D collider)
        {
            tempPoints.Clear();

            Debug.Log($"Path count: {collider.pathCount}");
            Debug.Log($"Total point count: {collider.pointCount}");

            for (int i = 0; i < collider.pathCount; i++)
            {
                int x = collider.GetPath(i, tempPoints);
                Debug.Log($"Path {i} has {collider.GetPathPointCount(i)} points; x = {x}");
            }

            return tempPoints;
        }
    }
}
