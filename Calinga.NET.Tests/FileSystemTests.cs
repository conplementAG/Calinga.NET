using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Calinga.NET.Infrastructure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calinga.NET.Tests
{
    [TestClass]
    public class FileSystemTests
    {
        private string _tempDir;
        private FileSystem _sut;

        [TestInitialize]
        public void Init()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"FileSystemTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
            _sut = new FileSystem();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [TestMethod]
        public async Task ReadAllTextAsync_ShouldAllowConcurrentReads()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "test.json");
            var content = "{\"key\": \"value\"}";
            await File.WriteAllTextAsync(filePath, content);

            // Act - Multiple concurrent reads should not throw
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => _sut.ReadAllTextAsync(filePath)))
                .ToList();

            Func<Task> act = async () => await Task.WhenAll(tasks);

            // Assert
            await act.Should().NotThrowAsync("concurrent reads should be allowed");

            // Verify all reads returned correct content
            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                result.Should().Be(content);
            }
        }

        [TestMethod]
        public async Task WriteAllTextAsync_ShouldCreateFileWithContent()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "write_test.json");
            var content = "{\"test\": \"data\"}";

            // Act
            await _sut.WriteAllTextAsync(filePath, content);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            var readContent = await File.ReadAllTextAsync(filePath);
            readContent.Should().Be(content);
        }

        [TestMethod]
        public async Task ReadAllTextAsync_ShouldReadFileContent()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "read_test.json");
            var content = "{\"hello\": \"world\"}";
            await File.WriteAllTextAsync(filePath, content);

            // Act
            var result = await _sut.ReadAllTextAsync(filePath);

            // Assert
            result.Should().Be(content);
        }

        [TestMethod]
        public void FileExists_ShouldReturnTrue_WhenFileExists()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "exists_test.json");
            File.WriteAllText(filePath, "test");

            // Act
            var result = _sut.FileExists(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void FileExists_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "nonexistent.json");

            // Act
            var result = _sut.FileExists(filePath);

            // Assert
            result.Should().BeFalse();
        }
    }
}
