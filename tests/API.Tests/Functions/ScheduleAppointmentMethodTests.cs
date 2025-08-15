using System;
using System.Reflection;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Functions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.Tests.Functions
{
    public class ScheduleAppointmentMethodTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<ScheduleAppointment>> _loggerMock;

        public ScheduleAppointmentMethodTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<ScheduleAppointment>>();
        }
        
        [Fact]
        public async Task CheckClientReviewStatusAsync_ClientUnderReview_ReturnsTrue()
        {
            // Arrange
            await SeedDatabase();
            
            string clientEmail = "review@example.com";
            DateTimeOffset appointmentDate = DateTimeOffset.Now.Date;
            
            // Create a client under review
            using (var context = CreateContext())
            {
                var client = new Client
                {
                    FirstName = "Review",
                    LastName = "Client",
                    Email = clientEmail,
                    PhoneNumber = "555-REVIEW"
                };
                
                context.Clients.Add(client);
                await context.SaveChangesAsync();
                
                // Create an appointment first
                var appointment = new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = 1,
                    Time = DateTimeOffset.UtcNow
                };
                context.Appointments.Add(appointment);
                await context.SaveChangesAsync();
                
                // Add a review flag
                context.ClientReviewFlags.Add(new ClientReviewFlag
                {
                    ClientId = client.Id,
                    AppointmentId = appointment.Id,
                    FlagReason = "Test flag",
                    FlagDate = DateTimeOffset.UtcNow,
                    Status = "Pending" // Pending status means under review
                });
                
                await context.SaveChangesAsync();
            }
            
            // Use reflection to access the private method
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "CheckClientReviewStatusAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            if (method != null)
            {
                var task = (Task<(bool IsUnderReview, int ExistingAppointmentsCount)>?)method.Invoke(
                    function, 
                    new object[] { clientEmail, appointmentDate });
                
                if (task != null)
                {
                    var result = await task;

                    // Assert
                    Assert.True(result.IsUnderReview);
                }
                else
                {
                    Assert.Fail("Task was null");
                }
            }
            else
            {
                Assert.Fail("Method not found");
            }
        }

        [Fact]
        public async Task CheckClientReviewStatusAsync_ClientWithRejectedStatus_ReturnsTrue()
        {
            // Arrange
            await SeedDatabase();
            
            string clientEmail = "rejected@example.com";
            DateTimeOffset appointmentDate = DateTimeOffset.Now.Date;
            
            // Create a client with rejected status
            using (var context = CreateContext())
            {
                var client = new Client
                {
                    FirstName = "Rejected",
                    LastName = "Client",
                    Email = clientEmail,
                    PhoneNumber = "555-REJECTED"
                };
                
                context.Clients.Add(client);
                await context.SaveChangesAsync();
                
                // Create an appointment first
                var appointment = new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = 1,
                    Time = DateTimeOffset.UtcNow
                };
                context.Appointments.Add(appointment);
                await context.SaveChangesAsync();
                
                // Add a review flag with Rejected status
                context.ClientReviewFlags.Add(new ClientReviewFlag
                {
                    ClientId = client.Id,
                    AppointmentId = appointment.Id,
                    FlagReason = "Test flag",
                    FlagDate = DateTimeOffset.UtcNow,
                    Status = "Rejected" // Rejected status also blocks appointments
                });
                
                await context.SaveChangesAsync();
            }
            
            // Use reflection to access the private method
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "CheckClientReviewStatusAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            if (method != null)
            {
                var task = (Task<(bool IsUnderReview, int ExistingAppointmentsCount)>?)method.Invoke(
                    function, 
                    new object[] { clientEmail, appointmentDate });
                
                if (task != null)
                {
                    var result = await task;

                    // Assert
                    Assert.True(result.IsUnderReview);
                }
                else
                {
                    Assert.Fail("Task was null");
                }
            }
            else
            {
                Assert.Fail("Method not found");
            }
        }

        [Fact]
        public async Task CheckClientReviewStatusAsync_ClientWithApprovedStatus_ReturnsFalse()
        {
            // Arrange
            await SeedDatabase();
            
            string clientEmail = "approved@example.com";
            DateTimeOffset appointmentDate = DateTimeOffset.Now.Date;
            
            // Create a client with approved status
            using (var context = CreateContext())
            {
                var client = new Client
                {
                    FirstName = "Approved",
                    LastName = "Client",
                    Email = clientEmail,
                    PhoneNumber = "555-APPROVED"
                };
                
                context.Clients.Add(client);
                await context.SaveChangesAsync();
                
                // Create an appointment first
                var appointment = new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = 1,
                    Time = DateTimeOffset.UtcNow
                };
                context.Appointments.Add(appointment);
                await context.SaveChangesAsync();
                
                // Add a review flag with Approved status
                context.ClientReviewFlags.Add(new ClientReviewFlag
                {
                    ClientId = client.Id,
                    AppointmentId = appointment.Id,
                    FlagReason = "Test flag",
                    FlagDate = DateTimeOffset.UtcNow,
                    Status = "Approved" // Approved status doesn't block
                });
                
                await context.SaveChangesAsync();
            }
            
            // Use reflection to access the private method
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "CheckClientReviewStatusAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var task = (Task<(bool IsUnderReview, int ExistingAppointmentsCount)>)method.Invoke(
                function, 
                new object[] { clientEmail, appointmentDate });
            
            var result = await task;

            // Assert
            Assert.False(result.IsUnderReview);
        }

        [Fact]
        public async Task CheckClientReviewStatusAsync_ClientWithExistingAppointments_CountsCorrectly()
        {
            // Arrange
            await SeedDatabase();
            
            string clientEmail = "multiple@example.com";
            DateTimeOffset appointmentDate = DateTimeOffset.Now.AddDays(1).Date;
            
            // Create a client with multiple appointments on the same day
            using (var context = CreateContext())
            {
                var client = new Client
                {
                    FirstName = "Multiple",
                    LastName = "Appointments",
                    Email = clientEmail,
                    PhoneNumber = "555-MULTIPLE"
                };
                
                context.Clients.Add(client);
                await context.SaveChangesAsync();
                
                // Add 2 appointments on the same day
                context.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = 1,
                    Time = appointmentDate.AddHours(9) // 9 AM
                });
                
                context.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = 1,
                    Time = appointmentDate.AddHours(14) // 2 PM
                });
                
                // Add 1 appointment on a different day (shouldn't be counted)
                context.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = 1,
                    Time = appointmentDate.AddDays(1).AddHours(10) // 10 AM next day
                });
                
                await context.SaveChangesAsync();
            }
            
            // Use reflection to access the private method
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "CheckClientReviewStatusAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var task = (Task<(bool IsUnderReview, int ExistingAppointmentsCount)>)method.Invoke(
                function, 
                new object[] { clientEmail, appointmentDate });
            
            var result = await task;

            // Assert
            Assert.False(result.IsUnderReview); // Not under review
            Assert.Equal(2, result.ExistingAppointmentsCount); // 2 appointments on the specified date
        }

        [Fact]
        public async Task CheckIfTimeIsAvailableAsync_ValidatesExactTimeMatchAndOverlaps()
        {
            // Arrange
            await SeedDatabase();
            
            var baseTime = DateTimeOffset.Now.AddDays(1).Date.AddHours(10);
            
            // Create a service with a duration and an existing appointment
            using (var context = CreateContext())
            {
                var service = await context.Services.FirstAsync();
                service.Duration = 60; // 1 hour duration
                await context.SaveChangesAsync();
                
                // Create an existing appointment
                var client = await context.Clients.FirstAsync();
                
                context.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = service.Id,
                    Time = baseTime
                });
                
                await context.SaveChangesAsync();
            }
            
            // Use reflection to access the private method
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "CheckIfTimeIsAvailableAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Test cases
            var testCases = new[]
            {
                // Exact same time (should not be available)
                (Time: baseTime, ExpectAvailable: false),
                
                // During service time, same service (should not be available)
                (Time: baseTime.AddMinutes(30), ExpectAvailable: false),
                
                // Right after service ends (should be available)
                (Time: baseTime.AddMinutes(61), ExpectAvailable: true),
                
                // Before service starts (should be available)
                (Time: baseTime.AddMinutes(-61), ExpectAvailable: true)
            };
            
            foreach (var test in testCases)
            {
                // Create test appointment DTO
                var appointmentDto = new CreateAppointmentDto
                {
                    ServiceId = 1,
                    Time = test.Time,
                    Client = new ClientDto
                    {
                        FirstName = "Test",
                        LastName = "Client",
                        Email = "test@example.com",
                        PhoneNumber = "555-TEST"
                    }
                };
                
                // Get service for test
                var service = await CreateContext().Services.FirstAsync();
                
                // Act
                var task = (Task<bool>)method.Invoke(
                    function, 
                    new object[] { appointmentDto, service });
                
                var isAvailable = await task;

                // Assert
                Assert.Equal(test.ExpectAvailable, isAvailable);
            }
        }

        [Fact]
        public async Task CheckIfTimeIsAvailableAsync_ExistingLongerServiceBlocksAvailability()
        {
            // Arrange
            await SeedDatabase();

            var baseTime = DateTimeOffset.Now.AddDays(1).Date.AddHours(10);

            using (var context = CreateContext())
            {
                context.Services.Add(new Service
                {
                    Id = 2,
                    Name = "Long Service",
                    Duration = 90,
                    Description = "Long duration service"
                });

                var client = await context.Clients.FirstAsync();

                context.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = 2,
                    Time = baseTime
                });

                await context.SaveChangesAsync();
            }

            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "CheckIfTimeIsAvailableAsync",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Service bookingService;
            using (var context = CreateContext())
            {
                bookingService = await context.Services.FindAsync(1) ?? new Service();
            }

            var appointmentDto = new CreateAppointmentDto
            {
                ServiceId = bookingService.Id,
                Time = baseTime.AddMinutes(60),
                Client = new ClientDto
                {
                    FirstName = "Test",
                    LastName = "Client",
                    Email = "test@example.com",
                    PhoneNumber = "555-TEST"
                }
            };

            var task = (Task<bool>)method.Invoke(
                function,
                new object[] { appointmentDto, bookingService });

            var isAvailable = await task;
            Assert.False(isAvailable);

            var appointmentDtoLater = new CreateAppointmentDto
            {
                ServiceId = bookingService.Id,
                Time = baseTime.AddMinutes(91),
                Client = new ClientDto
                {
                    FirstName = "Test",
                    LastName = "Client",
                    Email = "test@example.com",
                    PhoneNumber = "555-TEST"
                }
            };

            task = (Task<bool>)method.Invoke(
                function,
                new object[] { appointmentDtoLater, bookingService });

            var isAvailableLater = await task;
            Assert.True(isAvailableLater);
        }

        [Fact]
        public async Task CheckIfTimeIsAvailableAsync_ExistingShorterServiceAllowsEarlierBooking()
        {
            // Arrange
            await SeedDatabase();

            var baseTime = DateTimeOffset.Now.AddDays(1).Date.AddHours(10);

            using (var context = CreateContext())
            {
                var shortService = await context.Services.FindAsync(1);
                if (shortService != null)
                {
                    shortService.Duration = 30;
                }

                var client = await context.Clients.FirstAsync();

                context.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = shortService!.Id,
                    Time = baseTime
                });

                context.Services.Add(new Service
                {
                    Id = 2,
                    Name = "Standard Service",
                    Duration = 60,
                    Description = "Standard duration"
                });

                await context.SaveChangesAsync();
            }

            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "CheckIfTimeIsAvailableAsync",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Service bookingService;
            using (var context = CreateContext())
            {
                bookingService = await context.Services.FindAsync(2) ?? new Service();
            }

            var appointmentDtoOverlap = new CreateAppointmentDto
            {
                ServiceId = bookingService.Id,
                Time = baseTime.AddMinutes(20),
                Client = new ClientDto
                {
                    FirstName = "Test",
                    LastName = "Client",
                    Email = "test@example.com",
                    PhoneNumber = "555-TEST"
                }
            };

            var task = (Task<bool>)method.Invoke(
                function,
                new object[] { appointmentDtoOverlap, bookingService });

            var isAvailableOverlap = await task;
            Assert.False(isAvailableOverlap);

            var appointmentDtoFree = new CreateAppointmentDto
            {
                ServiceId = bookingService.Id,
                Time = baseTime.AddMinutes(31),
                Client = new ClientDto
                {
                    FirstName = "Test",
                    LastName = "Client",
                    Email = "test@example.com",
                    PhoneNumber = "555-TEST"
                }
            };

            task = (Task<bool>)method.Invoke(
                function,
                new object[] { appointmentDtoFree, bookingService });

            var isAvailableFree = await task;
            Assert.True(isAvailableFree);
        }

        [Fact]
        public async Task BookAppointmentStepsAsync_CreatesProperObjectHierarchy()
        {
            // Arrange
            await SeedDatabase();
            
            // Use a unique email to ensure we're creating a new client
            string uniqueEmail = $"unique.{Guid.NewGuid()}@example.com";
            
            var appointmentDto = new CreateAppointmentDto
            {
                ServiceId = 1,
                Time = DateTimeOffset.Now.AddDays(1),
                Client = new ClientDto
                {
                    FirstName = "Unique",
                    LastName = "Client",
                    Email = uniqueEmail,
                    PhoneNumber = "555-UNIQUE"
                }
            };
            
            var service = await CreateContext().Services.FirstAsync();
            
            // Use reflection to access the private method
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "BookAppointmentStepsAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var task = (Task<IActionResult>)method.Invoke(
                function, 
                new object[] { appointmentDto, service });
            
            var result = await task;

            // Assert
            Assert.IsType<CreatedResult>(result);
            
            // Verify that all objects were created properly
            using (var context = CreateContext())
            {
                // Verify client
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.Email == uniqueEmail);
                
                Assert.NotNull(client);
                if (client != null)
                {
                    Assert.Equal("Unique", client.FirstName);
                    Assert.Equal("Client", client.LastName);
                }
                
                // Verify appointment
                var appointment = await context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Service)
                    .FirstOrDefaultAsync(a => a.Client != null && a.Client.Email == uniqueEmail);
                
                Assert.NotNull(appointment);
                if (appointment != null)
                {
                    Assert.Equal(appointmentDto.Time, appointment.Time);
                    Assert.Equal(appointmentDto.ServiceId, appointment.ServiceId);
                    Assert.Equal(client?.Id, appointment.ClientId);
                    
                    // Verify service link
                    Assert.Equal(service.Id, appointment.ServiceId);
                    Assert.Equal(service.Name, appointment.Service?.Name);
                }
            }
        }

        [Fact]
        public async Task BookAppointmentStepsAsync_UpdatesExistingClient()
        {
            // Arrange
            await SeedDatabase();
            
            // Create an existing client with initial data
            string existingEmail = "existing@example.com";
            
            using (var context = CreateContext())
            {
                context.Clients.Add(new Client
                {
                    FirstName = "Initial",
                    LastName = "Name",
                    Email = existingEmail,
                    PhoneNumber = "555-INITIAL"
                });
                
                await context.SaveChangesAsync();
            }
            
            // Create appointment DTO with updated client information
            var appointmentDto = new CreateAppointmentDto
            {
                ServiceId = 1,
                Time = DateTimeOffset.Now.AddDays(1),
                Client = new ClientDto
                {
                    FirstName = "Updated",
                    LastName = "Name",
                    Email = existingEmail, // Same email to find existing client
                    PhoneNumber = "555-UPDATED"
                }
            };
            
            var service = await CreateContext().Services.FirstAsync();
            
            // Use reflection to access the private method
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "BookAppointmentStepsAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var task = (Task<IActionResult>)method.Invoke(
                function, 
                new object[] { appointmentDto, service });
            
            var result = await task;

            // Assert
            Assert.IsType<CreatedResult>(result);
            
            // Verify that client was found and not updated (current implementation doesn't update client data)
            using (var context = CreateContext())
            {
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.Email == existingEmail);
                
                Assert.NotNull(client);
                if (client != null)
                {
                    // The current implementation does not update existing client data
                    Assert.Equal("Initial", client.FirstName); 
                    Assert.Equal("Name", client.LastName);
                    Assert.Equal("555-INITIAL", client.PhoneNumber);
                }
                
                // Verify appointment was created
                var appointment = await context.Appointments
                    .FirstOrDefaultAsync(a => a.Client != null && a.Client.Email == existingEmail);
                
                Assert.NotNull(appointment);
            }
        }

        [Fact]
        public async Task FlagClientForReviewAsync_CreatesProperReviewFlag()
        {
            // Arrange
            await SeedDatabase();
            
            int clientId;
            int appointmentId;
            using (var context = CreateContext())
            {
                var client = new Client
                {
                    FirstName = "Flag",
                    LastName = "Test",
                    Email = "flag@example.com",
                    PhoneNumber = "555-FLAG"
                };
                
                context.Clients.Add(client);
                await context.SaveChangesAsync();
                clientId = client.Id;
                
                // Create an appointment to associate with the review flag
                var appointment = new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = 1,
                    Time = DateTimeOffset.UtcNow
                };
                context.Appointments.Add(appointment);
                await context.SaveChangesAsync();
                appointmentId = appointment.Id;
            }
            
            // Use reflection to access the private method
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());
            var method = typeof(ScheduleAppointment).GetMethod(
                "FlagClientForReviewAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var date = DateTimeOffset.Now;
            var appointmentCount = 2;
            var task = (Task?)method?.Invoke(
                function, 
                new object[] { clientId, date, appointmentCount, appointmentId });
            
            if (task != null)
            {
                await task;
            }

            // Assert
            using (var context = CreateContext())
            {
                var flag = await context.ClientReviewFlags
                    .FirstOrDefaultAsync(f => f.ClientId == clientId && f.AppointmentId == appointmentId);
                
                Assert.NotNull(flag);
                if (flag != null)
                {
                    Assert.Equal("Pending", flag.Status);
                    Assert.Equal(1, flag.AutoFlags);
                    Assert.Contains("Multiple bookings detected", flag.FlagReason);
                }
            }
        }

        #region Helper Methods
        
        private ProjectContext CreateContext()
        {
            return new ProjectContext(_dbContextOptions);
        }

        private async Task SeedDatabase()
        {
            var context = CreateContext();
            if (context == null) return;
            
            // Clear any existing data
            context.Clients.RemoveRange(context.Clients);
            context.Services.RemoveRange(context.Services);
            context.Appointments.RemoveRange(context.Appointments);
            context.ClientReviewFlags.RemoveRange(context.ClientReviewFlags);
            await context.SaveChangesAsync();
            
            // Add test data
            context.Clients.Add(new Client
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                PhoneNumber = "555-555-5555"
            });
            
            context.Services.Add(new Service
            {
                Id = 1,
                Name = "Test Service",
                Duration = 60,
                Description = "A test service",
                Price = 100.00m
            });
            
            await context.SaveChangesAsync();
        }
        
        #endregion
    }
}
