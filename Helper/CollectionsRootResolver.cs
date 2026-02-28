using Gelato.Decorators;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Gelato.Helper
{
    public class CollectionsRootResolver(
        Lazy<GelatoManager> manager,
        ILibraryManager libraryManager,
        ILogger<CollectionManagerDecorator> log)
    {
        private const string DefaultCollectionsName = "Streaming Catalogs";

        public async Task<Folder?> GetOrCreateCollectionsParentAsync(string collectionsPath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(collectionsPath))
            {
                throw new ArgumentException("collectionsPath must be set to a real, writable directory.", nameof(collectionsPath));
            }

            if (!Directory.Exists(collectionsPath))
            {
                Directory.CreateDirectory(collectionsPath);
            }

            var collectionsVirtualFolder = GetCollectionVirtualFolder();

            if (collectionsVirtualFolder is null)
            {
                await libraryManager.AddVirtualFolder(
                    name: DefaultCollectionsName,
                    collectionType: CollectionTypeOptions.boxsets,
                    new LibraryOptions
                    {
                        PathInfos = new[] { new MediaPathInfo { Path = collectionsPath } }
                    },
                    refreshLibrary: true).ConfigureAwait(false);
            }
            else
            {
                var hasPath = collectionsVirtualFolder.Locations?.Any(p =>
                    string.Equals(NormalizePath(p), NormalizePath(collectionsPath), StringComparison.OrdinalIgnoreCase)) == true;

                if (!hasPath)
                {
                    libraryManager.AddMediaPath(
                        collectionsVirtualFolder.Name,
                        new MediaPathInfo
                        {
                            Path = collectionsPath,
                        });
                }
            }

            collectionsVirtualFolder = GetCollectionVirtualFolder()
                ?? throw new InvalidOperationException($"Virtual folder '{DefaultCollectionsName}' (boxsets) was not created/found.");


            if (string.IsNullOrEmpty(collectionsVirtualFolder.ItemId))
            {
                throw new InvalidOperationException($"Virtual folder '{DefaultCollectionsName}' has an empty ItemId.");
            }

            var folder = libraryManager.GetItemById(collectionsVirtualFolder.ItemId) as Folder;

            return folder;
        }

        private static string NormalizePath(string path) => path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        private VirtualFolderInfo? GetCollectionVirtualFolder()
        {
            var virtualFolders = libraryManager.GetVirtualFolders();

            return virtualFolders.FirstOrDefault(v =>
                v.CollectionType == CollectionTypeOptions.boxsets &&
                string.Equals(v.Name, DefaultCollectionsName, StringComparison.Ordinal));
        }
    }
}
