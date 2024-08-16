using ManagedCommon;

using Microsoft.PowerToys.Settings.UI.Library;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using Wox.Plugin;

namespace PowerToys.Run.Tools
{
    /// <summary>
    /// Main class of this plugin that implement all used interfaces.
    /// </summary>
    public class Main : IPlugin, IContextMenu, IDisposable
    {
        private const long MaxFileSize = 1024 * 1024 * 1024;

        /// <summary>
        /// ID of the plugin.
        /// </summary>
        public static string PluginID => "ED86DFE645DC4D5EAA5BD68112F0CFF8";

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Tools";

        /// <summary>
        /// Description of the plugin.
        /// </summary>
        public string Description => "Tools Description";

        private PluginInitContext Context { get; set; }

        private string IconPath { get; set; }

        private bool Disposed { get; set; }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => throw new NotImplementedException();

        /// <summary>
        /// Return a filtered list, based on the given query.
        /// </summary>
        /// <param name="query">The query to filter the list.</param>
        /// <returns>A filtered list, can be empty when nothing was found.</returns>
        public List<Result> Query(Query query)
        {
            var search = query.Search;

            if (query.Terms.Count > 0 && query.Terms[0] == "md5f")
            {
                if (query.Terms.Count == 1)
                {
                    return
                    [
                        new Result
                         {
                             QueryTextDisplay = search,
                             IcoPath = IconPath,
                             Title = "获取文件MD5值",
                             SubTitle = "示例：md5f <文件路径>"
                         }
                    ];
                }

                var filePath = search.Substring(4).Trim(' ', '"');
                if (!System.IO.File.Exists(filePath))
                {
                    return
                    [
                        new Result
                        {
                            QueryTextDisplay = search,
                            IcoPath = IconPath,
                            Title = "文件不存在",
                            SubTitle = "文件路径：" + filePath
                        }
                    ];
                }

                var md5 = System.Security.Cryptography.MD5.Create();
                using var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                if (fs.Length > MaxFileSize)
                {
                    return
                    [
                        new Result
                        {
                            QueryTextDisplay = search,
                            IcoPath = IconPath,
                            Title = $"文件过大，最大不能超过：{ FormatFileSizeKeepTowDecimalPlaces(MaxFileSize) }",
                            SubTitle = "当前文件大小：" + FormatFileSizeKeepTowDecimalPlaces(fs.Length)
                        }
                    ];
                }
                var hash = md5.ComputeHash(fs);
                var md5Str = BitConverter.ToString(hash).Replace("-", "").ToLower();
                return
                [
                    new Result
                    {
                        QueryTextDisplay = search,
                        IcoPath = IconPath,
                        Title = "Copy to clipboard",
                        SubTitle = $"MD5: {md5Str}",
                        Action = _ =>
                        {
                            Clipboard.SetDataObject(md5Str);
                            return true;
                        },
                        ContextData = md5Str
                    },
                    new Result
                    {
                        QueryTextDisplay = search,
                        IcoPath = IconPath,
                        Title = "Copy to clipboard (upper)",
                        SubTitle = $"MD5: {md5Str.ToUpper()}",
                        Action = _ =>
                        {
                            Clipboard.SetDataObject(md5Str.ToUpper());
                            return true;
                        },
                        ContextData = md5Str.ToUpper()
                    }
                ];
            }

            return
               [
                   new Result
                    {
                        QueryTextDisplay = search,
                        IcoPath = IconPath,
                        Title = "获取文件MD5值",
                        SubTitle = "示例：md5f <文件路径>"
                    }
               ];
        }

        private static string FormatFileSizeKeepTowDecimalPlaces(long size)
        {
            if (size < 1024)
            {
                return size + "B";
            }
            else if (size < 1024 * 1024)
            {
                return Math.Round(size / 1024.0, 2) + "KB";
            }
            else if (size < 1024 * 1024 * 1024)
            {
                return Math.Round(size / 1024.0 / 1024.0, 2) + "MB";
            }
            else
            {
                return Math.Round(size / 1024.0 / 1024.0 / 1024.0, 2) + "GB";
            }
        }

        /// <summary>
        /// Initialize the plugin with the given <see cref="PluginInitContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="PluginInitContext"/> for this plugin.</param>
        public void Init(PluginInitContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());
        }

        /// <summary>
        /// Return a list context menu entries for a given <see cref="Result"/> (shown at the right side of the result).
        /// </summary>
        /// <param name="selectedResult">The <see cref="Result"/> for the list with context menu entries.</param>
        /// <returns>A list context menu entries.</returns>
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is string search)
            {
                return
                [
                    new ContextMenuResult
                    {
                        PluginName = Name,
                        Title = "Copy to clipboard (Ctrl+C)",
                        FontFamily = "Segoe MDL2 Assets",
                        Glyph = "\xE8C8", // Copy
                        AcceleratorKey = Key.C,
                        AcceleratorModifiers = ModifierKeys.Control,
                        Action = _ =>
                        {
                            Clipboard.SetDataObject(search);
                            return true;
                        },
                    }
                ];
            }

            return [];
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Wrapper method for <see cref="Dispose()"/> that dispose additional objects and events form the plugin itself.
        /// </summary>
        /// <param name="disposing">Indicate that the plugin is disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed || !disposing)
            {
                return;
            }

            if (Context?.API != null)
            {
                Context.API.ThemeChanged -= OnThemeChanged;
            }

            Disposed = true;
        }

        private void UpdateIconPath(Theme theme) => IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite ? "Images/tools.light.png" : "Images/tools.dark.png";

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);
    }
}
