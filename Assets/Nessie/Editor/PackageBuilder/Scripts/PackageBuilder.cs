using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using ReorderableList = UnityEditorInternal.ReorderableList;

namespace Nessie.Editor
{
    public class PackageBuilder : EditorWindow
    {
        private SerializedObject thisSO;

        public PackageData PackageData;
        private SerializedObject dataSO;

        [SerializeField] private Object[] assetList;
        private ReorderableList assetRList;

        private PackageData[] packageDatas;
        private string[] packageDataNames;
        private int selectedData;

        private Vector2 scrollPos;

        [MenuItem("Window/Nessie/Package Builder")]
        static void Init()
        {
            PackageBuilder window = (PackageBuilder)GetWindow(typeof(PackageBuilder));
            window.titleContent.text = "Nessie's Package Builder";
            Debug.Log(window.position.width);
            window.minSize = new Vector2(300, 100);
            window.Show();
        }

        private void OnEnable()
        {
            thisSO = new SerializedObject(this);

            assetRList = new ReorderableList(thisSO, thisSO.FindProperty(nameof(assetList)), true, false, true, true);
            assetRList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, new GUIContent("Asset References", "Assets packed into the Unity Package.")); };
            assetRList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                Rect elementRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

                SerializedProperty elementSP = assetRList.serializedProperty.GetArrayElementAtIndex(index);

                Object asset = EditorGUI.ObjectField(elementRect, elementSP.objectReferenceValue, typeof(Object), false);

                elementSP.objectReferenceValue = asset;

                //EditorGUI.PropertyField(testFieldRect, assetRList.serializedProperty.GetArrayElementAtIndex(index), label: new GUIContent());
            };

            packageDatas = AssetDatabase.FindAssets("t:PackageData").Select(path => AssetDatabase.LoadAssetAtPath<PackageData>(AssetDatabase.GUIDToAssetPath(path))).ToArray();
            packageDataNames = packageDatas.Select(d => d.name).ToArray();
            selectedData = Array.IndexOf(packageDatas, PackageData);
        }
        
        private void OnGUI()
        {
            thisSO.Update();

            EditorGUI.BeginChangeCheck();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            selectedData = EditorGUILayout.Popup("Package Data", selectedData, packageDataNames);

            if (selectedData != -1) PackageData = packageDatas[selectedData];

            if (EditorGUI.EndChangeCheck())
            {
                thisSO.ApplyModifiedProperties();

                if (PackageData != null)
                {
                    dataSO = new SerializedObject(PackageData);

                    assetList = FindAssets(PackageData);

                    ApplyAssets();
                }
            }

            if (PackageData != null)
            {
                if (dataSO == null) dataSO = new SerializedObject(PackageData);

                dataSO.Update();

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(dataSO.FindProperty(nameof(PackageData.PackageName)));
                EditorGUILayout.PropertyField(dataSO.FindProperty(nameof(PackageData.PackagePath)));
                EditorGUILayout.PropertyField(dataSO.FindProperty(nameof(PackageData.Flags)));

                if (EditorGUI.EndChangeCheck())
                {
                    dataSO.ApplyModifiedProperties();
                }

                if (GUILayout.Button("Build Package"))
                {
                    BuildPackage(PackageData);
                }

                EditorGUI.BeginChangeCheck();
                assetRList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                {
                    thisSO.ApplyModifiedProperties();

                    ApplyAssets();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void ApplyAssets()
        {
            SerializedProperty assetGUIDS = dataSO.FindProperty(nameof(PackageData.AssetGUIDs));
            SerializedProperty assetPaths = dataSO.FindProperty(nameof(PackageData.AssetPaths));
            assetGUIDS.arraySize = assetList.Length;
            assetPaths.arraySize = assetList.Length;
            for (int i = 0; i < assetList.Length; i++)
            {
                string path = AssetDatabase.GetAssetPath(assetList[i]);
                assetGUIDS.GetArrayElementAtIndex(i).stringValue = AssetDatabase.AssetPathToGUID(path);
                if (assetList[i] != null)
                    assetPaths.GetArrayElementAtIndex(i).stringValue = path;
            }

            dataSO.ApplyModifiedProperties();
        }

        private static Object[] FindAssets(PackageData data)
        {
            Object[] assets = new Object[data.AssetPaths.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(data.AssetGUIDs[i]);
                if (string.IsNullOrEmpty(path))
                    path = data.AssetPaths[i];

                assets[i] = AssetDatabase.LoadAssetAtPath<Object>(path);
            }
            return assets;
        }

        private static void BuildPackage(PackageData data)
        {
            string packagePath = data.PackagePath.EndsWith("/") ? data.PackagePath : $"{data.PackagePath}/"; // Add final forward slash if missing.
            string packageName = data.PackageName.EndsWith(".unitypackage") ? data.PackageName : $"{data.PackageName}.unitypackage"; // Add UnityPackage suffix if missing.
            ReadyPath(packagePath);
            string finalPath = AssetDatabase.GenerateUniqueAssetPath($"{packagePath}{packageName}"); // Format final path and prevent name collisions.

            string[] assetPaths = new string[data.AssetGUIDs.Length];
            for (int i = 0; i < assetPaths.Length; i++)
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(data.AssetGUIDs[i]);

            AssetDatabase.ExportPackage(assetPaths, finalPath, data.Flags);
            AssetDatabase.ImportAsset(data.PackagePath);

            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(finalPath);
        }

        private static bool ReadyPath(string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);

            bool exists = Directory.Exists(folderPath);
            if (!exists)
            {
                Directory.CreateDirectory(folderPath);
            }

            return !exists;
        }
    }
}