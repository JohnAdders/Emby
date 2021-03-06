﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class AlbumImageFromSongProvider : IDynamicImageProvider
    {
        public IEnumerable<ImageType> GetSupportedImages(IHasMetadata item)
        {
            return new List<ImageType> { ImageType.Primary };
        }

        public Task<DynamicImageResponse> GetImage(IHasMetadata item, ImageType type, CancellationToken cancellationToken)
        {
            var album = (MusicAlbum)item;

            var image = album.GetRecursiveChildren(i => !i.IsFolder)
                .Select(i => i.GetImageInfo(type, 0))
                .FirstOrDefault(i => i != null && i.IsLocalFile);

            var imagePath = image == null ? null : image.Path;

            return Task.FromResult(new DynamicImageResponse
            {
                Path = imagePath,
                HasImage = !string.IsNullOrEmpty(imagePath)
            });
        }

        public string Name
        {
            get { return "Image Extractor"; }
        }

        public bool Supports(IHasMetadata item)
        {
            return item is MusicAlbum;
        }
    }
}
