using ContactManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using ContactManager.Authorization;

// dotnet aspnet-codegenerator razorpage -m Contact -dc ApplicationDbContext -udl -outDir Pages\Contacts --referenceScriptLibraries

namespace ContactManager.Data
{
    public static class SeedData
    {
        public static void SeedDB(ApplicationDbContext context, string adminID)
        {
            if (context.Contact.Any())
            {
                return;   // DB has been seeded
            }

            context.Contact.AddRange(
                new Contact
                {
                    Name = "Debra Garcia",
                    Address = "1234 Main St",
                    City = "Redmond",
                    State = "WA",
                    Zip = "10999",
                    Email = "debra@example.com",
                    Status = ContactStatus.Approved, // Ensure this matches your ContactStatus enumeration or string values
                    OwnerID = adminID
                },
                new Contact
                {
                    Name = "John Doe",
                    Address = "5678 Elm St",
                    City = "Seattle",
                    State = "WA",
                    Zip = "98101",
                    Email = "john.doe@example.com",
                    Status = ContactStatus.Rejected, // Ensure this matches your ContactStatus enumeration or string values
                    OwnerID = adminID
                },
                new Contact
                {
                    Name = "Jane Smith",
                    Address = "9101 Maple Ave",
                    City = "Bellevue",
                    State = "WA",
                    Zip = "98004",
                    Email = "jane.smith@example.com",
                    Status = ContactStatus.Submitted, // Ensure this matches your ContactStatus enumeration or string values
                    OwnerID = adminID
                }
            );

            context.SaveChanges(); // Save changes to the database
        }

        public static async Task Initialize(IServiceProvider serviceProvider, string testUserPw)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // For sample purposes seed both with the same password.
                // Password is set with the following:
                // dotnet user-secrets set SeedUserPW <pw>
                // The admin user can do anything

                var adminID = await EnsureUser(serviceProvider, testUserPw, "admin@contoso.com");
                await EnsureRole(serviceProvider, adminID, ContactManager.Authorization.Constants.ContactAdministratorsRole);

                // allowed user can create and edit contacts that they create
                var managerID = await EnsureUser(serviceProvider, testUserPw, "manager@contoso.com");
                await EnsureRole(serviceProvider, managerID, ContactManager.Authorization.Constants.ContactManagersRole);

                SeedDB(context, adminID);
            }
        }

        private static async Task<string> EnsureUser(IServiceProvider serviceProvider,
                                                    string testUserPw, string UserName)
        {
            var userManager = serviceProvider.GetService<UserManager<IdentityUser>>();

            var user = await userManager.FindByNameAsync(UserName);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = UserName,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, testUserPw);
            }

            if (user == null)
            {
                throw new Exception("The password is probably not strong enough!");
            }

            return user.Id;
        }

        private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider,
                                                                      string uid, string role)
        {
            var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

            if (roleManager == null)
            {
                throw new Exception("roleManager null");
            }

            IdentityResult result;

            // Ensure the role exists
            if (!await roleManager.RoleExistsAsync(role))
            {
                result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    return result; // Return result if role creation failed
                }
            }

            var userManager = serviceProvider.GetService<UserManager<IdentityUser>>();

            if (userManager == null)
            {
                throw new Exception("userManager is null");
            }

            // Check if the user exists
            var user = await userManager.FindByIdAsync(uid);

            if (user == null)
            {
                // Create the user with the specified strong password if it does not exist
                user = new IdentityUser { Id = uid, UserName = "testuser", Email = "testuser@example.com" };
                result = await userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    // Log the errors or handle them accordingly
                    return result;
                }
            }

            // Add user to the role
            result = await userManager.AddToRoleAsync(user, role);
            return result;
        }

     }
}