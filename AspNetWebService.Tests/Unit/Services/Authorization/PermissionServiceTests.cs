﻿using AspNetWebService.Constants;
using AspNetWebService.Interfaces.Authorization;
using AspNetWebService.Interfaces.Logging;
using AspNetWebService.Interfaces.Utilities;
using AspNetWebService.Models.ServiceResultModels.Common;
using AspNetWebService.Services.Authorization;
using Moq;

namespace AspNetWebService.Tests.Unit.Services.Authorization
{
    /// <summary>
    ///     Unit tests for the <see cref="PermissionService"/> class.
    ///     This class contains test cases for various permission scenarios, verifying the 
    ///     behavior of the permission functionality.
    /// </summary>
    /// <remarks>
    ///     Author: Christian Briglio
    /// </remarks>
    [Trait("Category", "UnitTest")]
    public class PermissionServiceTests
    {
        private readonly Mock<IAuthorizationService> _authServiceMock;
        private readonly Mock<ILoggerService> _loggerServiceMock;
        private readonly Mock<IParameterValidator> _parameterValidatorMock;
        private readonly Mock<IServiceResultFactory> _serviceResultFactoryMock;
        private readonly PermissionService _permissionServiceMock;
        private const string UserId = "testid";

        /// <summary>
        ///     Initializes a new instance of the <see cref="PermissionServiceTests"/> class
        ///     and sets up necessary mock dependencies.
        /// </summary>
        public PermissionServiceTests()
        {
            _authServiceMock = new Mock<IAuthorizationService>();
            _loggerServiceMock = new Mock<ILoggerService>();
            _parameterValidatorMock = new Mock<IParameterValidator>();
            _serviceResultFactoryMock = new Mock<IServiceResultFactory>();

            _permissionServiceMock = new PermissionService(_authServiceMock.Object, _loggerServiceMock.Object, _parameterValidatorMock.Object, _serviceResultFactoryMock.Object);
        }


        /// <summary>
        ///     Verifies that calling the validate permissions method with null id throws an ArgumentNullException.
        /// </summary>
        /// <returns>
        ///     A task representing the asynchronous test operation.
        /// </returns>
        [Fact]
        public async Task ValidatePermission_NullId_ReturnsArgumentNullException()
        {
            // Arrange
            _parameterValidatorMock
                .Setup(x => x.ValidateNotNullOrEmpty(It.IsAny<string>(), It.IsAny<string>()))
                .Throws<ArgumentNullException>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _permissionServiceMock.ValidatePermissions(null));

            VerifyCallsToParameterService();
        }


        /// <summary>
        ///     Verifies that calling the validate permissions method with empty id throws an ArgumentNullException.
        /// </summary>
        /// <returns>
        ///     A task representing the asynchronous test operation.
        /// </returns>
        [Fact]
        public async Task ValidatePermission_EmptyId_ReturnsArgumentNullException()
        {
            // Arrange
            _parameterValidatorMock
                .Setup(x => x.ValidateNotNullOrEmpty(It.IsAny<string>(), It.IsAny<string>()))
                .Throws<ArgumentNullException>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _permissionServiceMock.ValidatePermissions(""));

            VerifyCallsToParameterService();
        }


        /// <summary>
        ///     Tests the validate permissions method to verify that it returns a failure result
        ///     and logs an authorization breach when the user lacks necessary permissions.
        /// </summary>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task ValidatePermissions_UserLacksPermission_ReturnsFailureAndLogsBreach()
        {
            // Arrange
            const string expectedErrorMessage = ErrorMessages.Authorization.Forbidden;

            ArrangeAuthorizationServiceMock(false);

            _serviceResultFactoryMock
                .Setup(s => s.GeneralOperationFailure(It.IsAny<string[]>()))
                .Returns(new ServiceResult
                {
                    Success = false,
                    Errors = { expectedErrorMessage }
                });

            // Act
            var result = await _permissionServiceMock.ValidatePermissions(UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains(expectedErrorMessage, result.Errors);

            _loggerServiceMock.Verify(l => l.LogAuthorizationBreach(), Times.Once);
            VerifyCallsToParameterService();
        }


        /// <summary>
        ///     Tests the validate permissions method to confirm it returns a success result
        ///     when the user has the required permissions.
        /// </summary>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task ValidatePermissions_UserHasPermission_ReturnsSuccess()
        {
            // Arrange
            ArrangeAuthorizationServiceMock(true);

            _serviceResultFactoryMock
                .Setup(s => s.GeneralOperationSuccess())
                .Returns(new ServiceResult { Success = true });

            // Act
            var result = await _permissionServiceMock.ValidatePermissions(UserId);

            // Assert
            Assert.True(result.Success);

            _loggerServiceMock.VerifyNoOtherCalls();
            VerifyCallsToParameterService();
        }


        /// <summary>
        ///     Configures the mock for <see cref="IAuthorizationService"/> to return a specified permission result
        ///     when validating a user's permission status.
        /// </summary>
        /// <param name="hasPermission">
        ///     Indicates whether the user has permission (true) or lacks permission (false).
        /// </param>
        private void ArrangeAuthorizationServiceMock(bool hasPermission)
        {
            _authServiceMock
                .Setup(a => a.ValidatePermission(UserId))
                .ReturnsAsync(hasPermission);
        }


        /// <summary>
        ///     Verifies that the <see cref="IParameterValidator"/> service's <c>ValidateNotNullOrEmpty</c> method 
        ///     was called exactly once during the test.
        /// </summary>
        private void VerifyCallsToParameterService()
        {
            _parameterValidatorMock.Verify(v => v.ValidateNotNullOrEmpty(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
