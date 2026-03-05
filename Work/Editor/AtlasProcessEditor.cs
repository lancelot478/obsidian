using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

public class AtlasProcessEditor
{
    [MenuItem("Tools/更新图集")]
    public static void UpdateAtlas()
    {
        var atlases = from guid in AssetDatabase.FindAssets("t:SpriteAtlas")
            let path = AssetDatabase.GUIDToAssetPath(guid)
            let atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path)
            where atlas != null
            select atlas;
        SpriteAtlasUtility.PackAtlases(atlases.ToArray(), EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();
    }
}