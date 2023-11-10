using System.IO;
using Newtonsoft.Json;
using Xunit;
using OfflineHandler;
using Moq;
using FlagsmithEngine.Environment.Models;
using System;
using System.Text;

namespace OfflineHandlerTests
{
    public class LocalFileHandlerTests
    {
        [Fact]
        public void GetEnvironment_ReturnsEnvironmentModel()
        {
            // Arrange
            Mock<IFileManager> mockFileManager = new Mock<IFileManager>();
            var expectedEnvironment = new EnvironmentModel
            {
                ID = 1,
                ApiKey = "test-api-key"
            };
            var expectedJson = JsonConvert.SerializeObject(expectedEnvironment);
            byte[] fakeFileBytes = Encoding.UTF8.GetBytes(expectedJson);

            MemoryStream fakeMemoryStream = new MemoryStream(fakeFileBytes);

            mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>()))
                   .Returns(() => new StreamReader(fakeMemoryStream));

            var expectedPath = "/path/to/environment.json";
            var localFileHandler = new LocalFileHandler(expectedPath, mockFileManager.Object);
            // Act
            var actualEnvironment = localFileHandler.GetEnvironment();
            Console.WriteLine(actualEnvironment);

            // Assert
            Assert.IsType<EnvironmentModel>(actualEnvironment);
            Assert.Equal(expectedEnvironment.ID, actualEnvironment.ID);
            Assert.Equal(expectedEnvironment.ApiKey, actualEnvironment.ApiKey);
            mockFileManager.Verify(fileManager => fileManager.StreamReader(expectedPath), Times.Once);
            mockFileManager.VerifyNoOtherCalls();
            fakeMemoryStream.Dispose();            
        }
    }
}