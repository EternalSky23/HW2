using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class CratePlacement
{
    // Both a complete navigation route and physical clearance are required.
    public static bool Reachable(Vector3 origin, Vector3 floor)
    {
        if (!NavMesh.SamplePosition(origin, out var start, 3, NavMesh.AllAreas)) return false;
        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(start.position, floor, NavMesh.AllAreas, path) ||
            path.status != NavMeshPathStatus.PathComplete) return false;
        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 segment = path.corners[i] - path.corners[i - 1];
            foreach (var hit in Physics.CapsuleCastAll(path.corners[i - 1] + Vector3.up * .45f,
                path.corners[i - 1] + Vector3.up * 1.5f, .28f, segment.normalized,
                segment.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (IsScenery(hit.collider)) return false;
        }
        return true;
    }

    private static bool IsScenery(Collider collider)
    {
        return collider.GetComponentInParent<PlayerController>() == null &&
            collider.GetComponentInParent<EnemyHealth>() == null &&
            collider.GetComponentInParent<DestructibleCrate>() == null &&
            collider.GetComponentInParent<HealItem>() == null;
    }

    public static bool OpenGround(Vector3 floor)
    {
        if (!NavMesh.FindClosestEdge(floor, out var edge, NavMesh.AllAreas) || edge.distance < 1.5f) return false;
        // Space for a one-metre crate plus room to approach it on every side.
        foreach (var collider in Physics.OverlapBox(floor + Vector3.up * 1.1f,
            new Vector3(1.35f, .85f, 1.35f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            if (IsScenery(collider)) return false;
        return true;
    }

    public static List<Vector3> Layout(Vector3 spawn)
    {
        Physics.SyncTransforms();
        var points = new List<Vector3>();
        for (int i = 0; i < 300 && points.Count < 6; i++)
        {
            float angle = i * 137.5f * Mathf.Deg2Rad;
            float radius = 5 + (i % 7) * 2;
            Vector3 candidate = spawn + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
            if (!NavMesh.SamplePosition(candidate, out var nav, 1.5f, NavMesh.AllAreas)) continue;
            Vector3 floor = nav.position;
            if (!Physics.Raycast(floor + Vector3.up * .8f, Vector3.down, out var ground, 1.5f,
                ~0, QueryTriggerInteraction.Ignore) || ground.normal.y < .9f ||
                Mathf.Abs(ground.point.y - floor.y) > .3f) continue;
            if (!OpenGround(floor) || !Reachable(spawn, floor)) continue;
            bool nearAnother = false;
            foreach (Vector3 point in points) if (Vector3.Distance(point, floor) < 3.5f) nearAnother = true;
            if (nearAnother) continue;
            points.Add(floor);
        }
        return points;
    }
}
