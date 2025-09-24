using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VertexAnimation.Editor
{
    public class EditorPrefsManager
    {
        private const string LAST_OUTPUT_FOLDER_KEY = "VAT_LastOutputFolder";
        private const string LAST_VA_SHADER_KEY = "VAT_LastVAShader";
        private const string LAST_VA_SHADER_COUNT_KEY = "VAT_LastVAShaderCount";
        
        public string GetLastOutputFolder()
        {
            string lastOutputFolder = EditorPrefs.GetString(LAST_OUTPUT_FOLDER_KEY, "Assets/");
            if (System.IO.Directory.Exists(lastOutputFolder))
            {
                return lastOutputFolder;
            }
            return "Assets/";
        }
        
        public void SetLastOutputFolder(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                EditorPrefs.SetString(LAST_OUTPUT_FOLDER_KEY, path);
            }
        }
        
        public Shader[] GetLastVAShaders()
        {
            var key = LAST_VA_SHADER_KEY;
            var countKey = LAST_VA_SHADER_COUNT_KEY;

            int shaderCount = EditorPrefs.GetInt(countKey, 0);
            List<Shader> shaders = new List<Shader>();
            for (int i = 0; i < shaderCount; i++)
            {
                string shaderPath = EditorPrefs.GetString(key + i, "");
                if (!string.IsNullOrEmpty(shaderPath))
                {
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                    if (shader != null)
                    {
                        shaders.Add(shader);
                    }
                }
            }
            return shaders.ToArray();
        }
        
        public void SetLastVAShaders(Shader[] shaders)
        {
            if (shaders != null)
            {
                var key = LAST_VA_SHADER_KEY;
                var countKey = LAST_VA_SHADER_COUNT_KEY;

                for (int i = 0; i < shaders.Length; i++)
                {
                    string shaderPath = AssetDatabase.GetAssetPath(shaders[i]);
                    EditorPrefs.SetString(key + i, shaderPath);
                }
                EditorPrefs.SetInt(countKey, shaders.Length);
            }
            else
            {
                ClearLastVAShaders();
            }
        }
        
        public void ClearLastVAShaders()
        {
            var key = LAST_VA_SHADER_KEY;
            var countKey = LAST_VA_SHADER_COUNT_KEY;

            int shaderCount = EditorPrefs.GetInt(countKey, 0);
            for (int i = 0; i < shaderCount; i++)
            {
                EditorPrefs.DeleteKey(key + i);
            }

            EditorPrefs.SetInt(countKey, 0);
        }
        
        public void ClearAllPreferences()
        {
            EditorPrefs.DeleteKey(LAST_OUTPUT_FOLDER_KEY);
            ClearLastVAShaders();
        }
    }
}
