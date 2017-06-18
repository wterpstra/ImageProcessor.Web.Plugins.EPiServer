using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using FluentAssertions;
using ImageProcessor.Web.Services;
using Moq;
using NUnit.Framework;

namespace ImageProcessor.Web.Plugins.EPiServer.Tests
{
    [TestFixture]
    public class EPiServerImageServiceTests
    {
        [Test]
        public void IsValidRequest_CurrentContentIsImageData_ReturnsTrue()
        {
            // ARRANGE
            var imageData = new Mock<ImageData>().Object;

            var pageRouteHelper = CreatePageRouteHelper(imageData);

            var serviceLocator = new Mock<IServiceLocator>();
            serviceLocator.Setup(x => x.GetInstance<IPageRouteHelper>()).Returns(pageRouteHelper);
            ServiceLocator.SetLocator(serviceLocator.Object);

            var sut = CreateEPiServerImageService();

            // ACT
            var result = sut.IsValidRequest(null);

            // ASSERT
            result.Should().BeTrue();
        }

        [Test]
        public void IsValidRequest_CurrentContentIsPageData_ReturnsFalse()
        {
            // ARRANGE
            var imageData = new Mock<PageData>().Object;

            var pageRouteHelper = CreatePageRouteHelper(imageData);

            var serviceLocator = new Mock<IServiceLocator>();
            serviceLocator.Setup(x => x.GetInstance<IPageRouteHelper>()).Returns(pageRouteHelper);
            ServiceLocator.SetLocator(serviceLocator.Object);

            var sut = CreateEPiServerImageService();

            // ACT
            var result = sut.IsValidRequest(null);

            // ASSERT
            result.Should().BeFalse();
        }

        [Test]
        public void IsValidRequest_CurrentContentIsNull_ReturnsFalse()
        {
            // ARRANGE
            var pageRouteHelper = CreatePageRouteHelper<IContent>(null);

            var serviceLocator = new Mock<IServiceLocator>();
            serviceLocator.Setup(x => x.GetInstance<IPageRouteHelper>()).Returns(pageRouteHelper);
            ServiceLocator.SetLocator(serviceLocator.Object);

            var sut = CreateEPiServerImageService();

            // ACT
            var result = sut.IsValidRequest(null);

            // ASSERT
            result.Should().BeFalse();
        }

        [Test]
        public async Task GetImage_ValidImageData_ReturnsImageDataMemoryStream()
        {
            // ARRANGE
            var test123 = "test123";

            var blob = new Mock<Blob>(new Uri("/notnecessaryfortest", UriKind.Relative));
            blob.Setup(x => x.OpenRead()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(test123)));

            var imageData = new Mock<ImageData>();
            imageData.Setup(x => x.BinaryData).Returns(blob.Object);

            var pageRouteHelper = CreatePageRouteHelper(imageData.Object);

            var serviceLocator = new Mock<IServiceLocator>();
            serviceLocator.Setup(x => x.GetInstance<IPageRouteHelper>()).Returns(pageRouteHelper);
            ServiceLocator.SetLocator(serviceLocator.Object);

            var sut = CreateEPiServerImageService();

            // ACT
            var result = await sut.GetImage(null);

            // ASSET
            result.Should().NotBeNullOrEmpty();
            var resultStr = Encoding.UTF8.GetString(result);
            resultStr.Should().NotBeNullOrEmpty().And.Be(test123);
        }

        private EPiServerImageService CreateEPiServerImageService(IImageService fallBackImageService = null, bool enableLocalFallBack = false)
        {
            return new EPiServerImageService(fallBackImageService, enableLocalFallBack);
        }

        private IPageRouteHelper CreatePageRouteHelper<T>(T content) where T : IContent
        {
            var pageRouteHelper = new Mock<IPageRouteHelper>();

            pageRouteHelper.Setup(x => x.Content).Returns(content);
            pageRouteHelper.Setup(x => x.ContentLink).Returns(content?.ContentLink);

            var pageData = content as PageData;
            pageRouteHelper.Setup(x => x.Page).Returns(pageData);
            pageRouteHelper.Setup(x => x.PageLink).Returns(pageData?.PageLink);
            
            return pageRouteHelper.Object;
        }

    }
}
