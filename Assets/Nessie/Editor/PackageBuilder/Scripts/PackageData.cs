using UnityEngine;

namespace Nessie.Editor
{
    [CreateAssetMenu(fileName = "Package_Data", menuName = "Nessie/Package Builder/Data", order = 1)]
    public class PackageData : ScriptableObject
    {
        public UnityEditor.ExportPackageOptions Flags;
        public string PackageName = "";
        public string PackagePath = "Assets/Nessie/Editor/PackageBuilder/Packages/";
        public string[] AssetGUIDs = new string[0];
        public string[] AssetPaths = new string[0];
    }
}