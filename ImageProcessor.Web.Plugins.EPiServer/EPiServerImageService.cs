using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using ImageProcessor.Web.Services;

namespace ImageProcessor.Web.Plugins.EPiServer
{
    /// <summary>
    /// An ImageProcessor.Web.Services.IImageService for connecting ImageProcessor to the EPiServer ImageData blobs. 
    /// Has support to fall back to another IImageService (by default the ImageProcessor.Web.Services.LocalFileImageService) if no ImageData blob is found.
    /// </summary>
    public class EPiServerImageService : IImageService
    {
        private readonly bool _fallBackEnabled;
        private readonly IImageService _fallBackImageService;

        /// <summary>
        /// Constructor used for unit testing
        /// </summary>
        public EPiServerImageService(IImageService fallbackImageService, bool fallbackEnabled)
        {
            _fallBackImageService = fallbackImageService;
            _fallBackEnabled = false;
        }

        /// <summary>
        /// Parameterless constructor used by ImageProcessor
        /// </summary>
        [ExcludeFromCodeCoverage]
        public EPiServerImageService()
        {
            var typeStr = Settings?["FallBackImageService"];

            if (!string.IsNullOrWhiteSpace(typeStr))
            {
                var type = Type.GetType(typeStr, false);
                
                if (typeof(IImageService).IsAssignableFrom(type))
                    _fallBackImageService = (IImageService) Activator.CreateInstance(type);
            }

            if (_fallBackImageService == null)
                _fallBackImageService = new LocalFileImageService();

            _fallBackEnabled = bool.TryParse(Settings?["EnableFallBack"], out bool enableFallBack) && enableFallBack;
        }

        /// <summary>
        /// Checks with the IPageRouteHelper whether the current request resolves to a ImageData object. 
        /// When fallBackEnabled is true it will check with the fallbackImageService if it can resolve it.
        /// </summary>
        public bool IsValidRequest(string path)
        {
            var pageRouteHelper = ServiceLocator.Current.GetInstance<IPageRouteHelper>();
            var imageData = pageRouteHelper.Content as ImageData;

            if (imageData != null)
                return true;

            if (!_fallBackEnabled)
                return false;

            try
            {
                if (_fallBackImageService.IsFileLocalService)
                    path = HttpContext.Current.Server.MapPath(path);

                return _fallBackImageService.IsValidRequest(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Will resolve the image bytes from the ImageData blobs. When not found it will make the fallBackImageService resolve the image bytes instead.
        /// </summary>
        public async Task<byte[]> GetImage(object id)
        {
            var pageRouteHelper = ServiceLocator.Current.GetInstance<IPageRouteHelper>();

            var imageData = pageRouteHelper.Content as ImageData;

            if (imageData == null)
            {
                if (_fallBackImageService.IsFileLocalService)
                    id = HttpContext.Current.Server.MapPath(id.ToString());

                return await _fallBackImageService.GetImage(id).ConfigureAwait(false);
            }

            using (var ms = new MemoryStream())
            {
                var imageStream = imageData.BinaryData.OpenRead();
                await imageStream.CopyToAsync(ms).ConfigureAwait(false);
                return ms.ToArray();
            }
        }

        [ExcludeFromCodeCoverage]
        public string Prefix { get; set; } = string.Empty;

        [ExcludeFromCodeCoverage]
        public bool IsFileLocalService => false;

        [ExcludeFromCodeCoverage]
        public Dictionary<string, string> Settings { get; set; }

        [ExcludeFromCodeCoverage]
        public Uri[] WhiteList { get; set; }
    }
}