using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
namespace AvatarPosingStationUtilities
{
    public class AvatarSDKDownloaderForWorld : EditorWindow
    {
        private string vrchatBaseVersion = "取得中...";
        private bool isDownloading = false;
        private string downloadStatus = "";
        private static readonly HttpClient httpClient = new HttpClient();

        [MenuItem("AvatarPosingStationUtilities/AvatarSDK Downloader for World")]
        public static void ShowWindow()
        {
            GetWindow<AvatarSDKDownloaderForWorld>("Avatar SDK Downloader for World");
        }

        private void OnEnable()
        {
            GetVRChatBaseVersion();
        }

        private void OnGUI()
        {
            GUILayout.Label("VRChat SDK Downloader for World", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("com.vrchat.base バージョン:", vrchatBaseVersion);

            GUILayout.Space(10);

            if (GUILayout.Button("バージョンを再取得"))
            {
                GetVRChatBaseVersion();
            }

            GUILayout.Space(20);

            GUI.enabled = !isDownloading && !string.IsNullOrEmpty(vrchatBaseVersion) &&
                         vrchatBaseVersion != "取得中..." &&
                         vrchatBaseVersion != "com.vrchat.base パッケージが見つかりません" &&
                         !vrchatBaseVersion.StartsWith("エラー:");

            if (GUILayout.Button("VRChat Avatar SDK をダウンロード"))
            {
                DownloadAvatarSDK();
            }

            GUI.enabled = true;

            if (isDownloading)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("ダウンロード状況:", downloadStatus);

                // プログレスバー風の表示
                Rect rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(rect, 0.5f, "ダウンロード中...");
            }
            else if (!string.IsNullOrEmpty(downloadStatus))
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("状況:", downloadStatus);
            }
        }

        private void GetVRChatBaseVersion()
        {
            string packagePath = Path.Combine(Application.dataPath, "..", "Packages", "com.vrchat.base", "package.json");

            if (File.Exists(packagePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(packagePath);
                    JObject packageJson = JObject.Parse(jsonContent);
                    vrchatBaseVersion = packageJson["version"]?.ToString() ?? "バージョン情報が見つかりません";
                }
                catch (System.Exception ex)
                {
                    vrchatBaseVersion = $"エラー: {ex.Message}";
                    Debug.LogError($"VRChat Base パッケージのバージョン取得に失敗しました: {ex.Message}");
                }
            }
            else
            {
                vrchatBaseVersion = "com.vrchat.base パッケージが見つかりません";
                Debug.LogWarning($"パッケージファイルが見つかりません: {packagePath}");
            }
        }

        private async void DownloadAvatarSDK()
        {
            if (isDownloading) return;

            try
            {
                isDownloading = true;
                downloadStatus = "ダウンロードを開始しています...";

                string version = vrchatBaseVersion;
                string downloadUrl = $"https://github.com/vrchat/packages/releases/download/{version}/com.vrchat.avatars-{version}.zip";
                string assetsPath = Path.Combine(Application.dataPath);
                string tempPath = Path.Combine(Path.GetTempPath(), $"vrchat-avatars-{version}.zip");

                Debug.Log($"ダウンロード URL: {downloadUrl}");
                Debug.Log($"一時保存先: {tempPath}");

                downloadStatus = "ファイルをダウンロード中...";
                Repaint();

                // ファイルをダウンロード
                using (var response = await httpClient.GetAsync(downloadUrl))
                {
                    response.EnsureSuccessStatusCode();

                    using (var fileStream = new FileStream(tempPath, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                downloadStatus = "ZIPファイルを展開中...";
                Repaint();

                // ZIPファイルをcom.vrchat.avatarsフォルダに展開
                string avatarsFolderPath = Path.Combine(assetsPath, "com.vrchat.avatars");

                // 既存のcom.vrchat.avatarsフォルダが存在する場合は削除
                if (Directory.Exists(avatarsFolderPath))
                {
                    downloadStatus = "既存のフォルダを削除中...";
                    Repaint();

                    try
                    {
                        Directory.Delete(avatarsFolderPath, true);
                        Debug.Log("既存のcom.vrchat.avatarsフォルダを削除しました。");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"既存フォルダの削除に失敗しました: {ex.Message}");
                    }
                }

                downloadStatus = "ZIPファイルを展開中...";
                Repaint();

                using (var archive = ZipFile.OpenRead(tempPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;

                        string destinationPath = Path.Combine(avatarsFolderPath, entry.FullName);
                        string destinationDir = Path.GetDirectoryName(destinationPath);

                        // ディレクトリが存在しない場合は作成
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        // ファイルを展開（ディレクトリエントリの場合はスキップ）
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }

                // 一時ファイルを削除
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                downloadStatus = "不要なファイルを削除中...";
                Repaint();

                // 必要なファイル以外を削除
                CleanupUnnecessaryFiles(avatarsFolderPath);

                downloadStatus = $"完了: VRChat Avatar SDK v{version} をダウンロードし、必要なファイルのみを残して展開しました。";

                // Unity のアセットデータベースを更新
                AssetDatabase.Refresh();

                Debug.Log($"VRChat Avatar SDK v{version} のダウンロードと展開が完了しました。");
            }
            catch (System.Exception ex)
            {
                downloadStatus = $"エラー: {ex.Message}";
                Debug.LogError($"VRChat Avatar SDK のダウンロードに失敗しました: {ex.Message}");
            }
            finally
            {
                isDownloading = false;
                Repaint();
            }
        }

        /// <summary>
        /// 必要なファイル以外を削除する処理
        /// </summary>
        /// <param name="avatarsFolderPath">com.vrchat.avatarsフォルダのパス</param>
        private void CleanupUnnecessaryFiles(string avatarsFolderPath)
        {
            if (!Directory.Exists(avatarsFolderPath))
            {
                Debug.LogError("Avatar SDKフォルダが見つかりません: " + avatarsFolderPath);
                return;
            }

            // 保持するファイルのホワイトリスト（相対パス、メタファイルは自動追加）
            var whitelistFiles = new string[]
            {
            // Runtime DLL
            "Runtime/VRCSDK/Plugins/VRCSDK3A.dll",
            
            // Editor Scripts - Animator Components
            "Editor/VRCSDK/SDK3A/Components3/VRCAnimatorLocomotionControlEditor.cs",
            "Editor/VRCSDK/SDK3A/Components3/VRCAnimatorPlayAudioEditor.cs",
            "Editor/VRCSDK/SDK3A/Components3/VRCAnimatorRemeasureAvatarEditor.cs",
            "Editor/VRCSDK/SDK3A/Components3/VRCAnimatorTemporaryPoseSpaceEditor.cs",
            "Editor/VRCSDK/SDK3A/Components3/VRCAnimatorTrackingControlEditor.cs"
            };

            // ホワイトリストからHashSetを作成（メタファイルも自動追加）
            var filesToKeep = new HashSet<string>();
            foreach (string file in whitelistFiles)
            {
                string normalizedPath = file.Replace('\\', '/');
                filesToKeep.Add(normalizedPath);
                filesToKeep.Add(normalizedPath + ".meta"); // メタファイルも自動追加
            }

            try
            {
                // すべてのファイルを取得
                var allFiles = Directory.GetFiles(avatarsFolderPath, "*", SearchOption.AllDirectories);

                // ファイルの削除
                foreach (string filePath in allFiles)
                {
                    string relativePath = Path.GetRelativePath(avatarsFolderPath, filePath).Replace('\\', '/');

                    if (!filesToKeep.Contains(relativePath))
                    {
                        File.Delete(filePath);
                        Debug.Log($"削除されたファイル: {relativePath}");
                    }
                    else
                    {
                        Debug.Log($"保持されたファイル: {relativePath}");
                    }
                }

                // 空のディレクトリを削除
                CleanupEmptyDirectories(avatarsFolderPath);

                Debug.Log("不要なファイルの削除が完了しました。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ファイルのクリーンアップ中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 空のディレクトリを削除する
        /// </summary>
        /// <param name="rootPath">ルートパス</param>
        private void CleanupEmptyDirectories(string rootPath)
        {
            try
            {
                var allDirectories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                                              .OrderByDescending(d => d.Length) // 下位ディレクトリから処理
                                              .ToArray();

                foreach (string directoryPath in allDirectories)
                {
                    if (Directory.Exists(directoryPath))
                    {
                        try
                        {
                            // ディレクトリが空かチェック（ファイルもサブディレクトリもない）
                            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
                            {
                                string relativePath = Path.GetRelativePath(rootPath, directoryPath).Replace('\\', '/');
                                Directory.Delete(directoryPath, false);
                                Debug.Log($"削除された空ディレクトリ: {relativePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            string relativePath = Path.GetRelativePath(rootPath, directoryPath).Replace('\\', '/');
                            Debug.LogWarning($"ディレクトリの削除に失敗: {relativePath} - {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"空ディレクトリの削除中にエラーが発生しました: {ex.Message}");
            }
        }
    }
}