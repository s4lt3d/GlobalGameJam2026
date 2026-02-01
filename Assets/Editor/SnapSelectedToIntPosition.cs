using UnityEditor;
using UnityEngine;

public static class SnapSelectedToIntPosition
{
    [MenuItem("Tools/Snap Selected To Int Position %#i")]
    private static void SnapSelection()
    {
        var transforms = Selection.transforms;
        if (transforms == null || transforms.Length == 0)
            return;

        foreach (var t in transforms)
        {
            if (t == null)
                continue;

            Undo.RecordObject(t, "Snap Selected To Int Position");
            Vector3 p = t.position;
            t.position = new Vector3(
                Mathf.Round(p.x),
                Mathf.Round(p.y),
                Mathf.Round(p.z));
            EditorUtility.SetDirty(t);
        }
    }
}
