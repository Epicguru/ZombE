using System.Collections.Generic;
using UnityEngine;

namespace ZombE
{
    public static class ColliderExtensions
    {
        private static List<Vector2> path = new List<Vector2>();

        /// <summary>
        /// Gets the relevant edge points on a collider surface, when viewed from a particulat point.
        /// Does not occlude points, the view point is just used for circle colliders.
        /// </summary>
        /// <param name="collider">The collider to get edge points from. Not all 2D collider types are supported.</param>
        /// <param name="castPoint">The point from which the collider is 'viewed'. Only used for circle colliders.</param>
        /// <param name="points">A list of points. Must not be null. Points are added on to the end of the list.</param>
        /// <returns>The number of points added.</returns>
        public static int GetCastPoints(this Collider2D collider, Vector2 castPoint, List<Vector2> points)
        {
            if (collider == null)
                return 0;

            if (points == null)
                return 0;

            int startCount = points.Count;

            var type = collider.GetType();
            var name = type.Name;
            switch (name)
            {
                case "BoxCollider2D":

                    BoxCollider2D box = collider as BoxCollider2D;

                    Vector2 topLeft = box.transform.TransformPoint(box.offset + new Vector2(box.size.x * -0.5f, box.size.y * 0.5f));
                    points.Add(topLeft);

                    Vector2 topRight = box.transform.TransformPoint(box.offset + new Vector2(box.size.x * 0.5f, box.size.y * 0.5f));
                    points.Add(topRight);

                    Vector2 bottomLeft = box.transform.TransformPoint(box.offset + new Vector2(box.size.x * -0.5f, box.size.y * -0.5f));
                    points.Add(bottomLeft);

                    Vector2 bottomRight = box.transform.TransformPoint(box.offset + new Vector2(box.size.x * 0.5f, box.size.y * -0.5f));
                    points.Add(bottomRight);

                    break;

                case "EdgeCollider2D":

                    EdgeCollider2D edge = collider as EdgeCollider2D;

                    foreach (var point in edge.points)
                    {
                        Vector2 worldPoint = edge.transform.TransformPoint(point.x, point.y, 0f);
                        points.Add(worldPoint);
                    }

                    break;

                case "CircleCollider2D":

                    const float POINTS_PER_QUADRANT = 1f;
                    const float RADIUS_MULTI = Mathf.PI * 2f;

                    CircleCollider2D circle = collider as CircleCollider2D;

                    Vector2 center = circle.transform.TransformPoint(circle.offset);

                    // r = the circle radius. Uses a scale aproximation.
                    float r = circle.radius * circle.transform.lossyScale.x;
                    if ((castPoint - center).sqrMagnitude <= r * r)
                        break;

                    float dst = (castPoint - center).magnitude;

                    float tangentChangeAngle = Mathf.Acos(r / dst);

                    float angleFromCenterToP = Mathf.Atan2(castPoint.y - center.y, castPoint.x - center.x);

                    float t1Angle = angleFromCenterToP + tangentChangeAngle;
                    float t2Angle = angleFromCenterToP - tangentChangeAngle;

                    Vector2 t1Offset = new Vector2(Mathf.Cos(t1Angle), Mathf.Sin(t1Angle)) * r;
                    Vector2 t2Offset = new Vector2(Mathf.Cos(t2Angle), Mathf.Sin(t2Angle)) * r;
                    Vector2 t1 = center + t1Offset;
                    Vector2 t2 = center + t2Offset;

                    points.Add(t1);

                    float angleDiff = Vector2.Angle(t1Offset, t2Offset);
                    int extraPointCount = Mathf.CeilToInt(POINTS_PER_QUADRANT * (angleDiff / 90f) * (RADIUS_MULTI * r));
                    for (int i = 0; i < extraPointCount; i++)
                    {
                        float p = (float)(i + 0.5f) / extraPointCount;
                        float angle = Mathf.Lerp(t1Angle, t2Angle, p);
                        Vector2 tInter = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                        points.Add(tInter);
                    }

                    points.Add(t2);

                    break;

                case "PolygonCollider2D":

                    PolygonCollider2D poly = collider as PolygonCollider2D;
                    var trs = poly.transform;
                    path.Clear();

                    int pathCount = poly.pathCount;
                    for (int i = 0; i < pathCount; i++)
                    {
                        int pathLength = poly.GetPath(i, path);
                        for (int j = 0; j < pathLength; j++)
                        {
                            Vector2 point = path[j];
                            points.Add(trs.TransformPoint(point));
                        }
                    }

                    break;

                case "CompositeCollider2D":

                    CompositeCollider2D comp = collider as CompositeCollider2D;
                    trs = comp.transform;
                    path.Clear();

                    pathCount = comp.pathCount;
                    for (int i = 0; i < pathCount; i++)
                    {
                        int pathLength = comp.GetPath(i, path);
                        for (int j = 0; j < pathLength; j++)
                        {
                            Vector2 point = path[j];
                            points.Add(trs.TransformPoint(point));
                        }
                    }

                    break;

                default:
                    //Debug.LogWarning($"Collider of type {name} ({type.FullName}) is not supported yet.");
                    break;
            }

            return points.Count - startCount;
        }

        /// <summary>
        ///  Gets the world space center of the collider2D.
        /// </summary>
        public static Vector2 GetCentre(this Collider2D collider)
        {
            var type = collider.GetType();
            var name = type.Name;
            switch (name)
            {
                default:
                    return collider.transform.TransformPoint(collider.offset);
            }
        }
    }

}
