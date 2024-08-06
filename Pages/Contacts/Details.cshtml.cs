using ContactManager.Authorization;
using ContactManager.Data;
using ContactManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ContactManager.Pages.Contacts
{
    public class DetailsModel : DI_BasePageModel
    {
        public DetailsModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<IdentityUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public Contact Contact { get; set; }
        public bool CanApprove { get; private set; }
        public bool CanReject { get; private set; }
        public bool CanEdit { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Fetch the contact
            Contact = await Context.Contact.FirstOrDefaultAsync(m => m.ContactId == id);

            if (Contact == null)
            {
                return NotFound();
            }

            // Authorization checks
            var currentUserId = UserManager.GetUserId(User);

            // Check if user can approve or reject the contact
            CanApprove = (await AuthorizationService.AuthorizeAsync(User, Contact, ContactOperations.Approve)).Succeeded;
            CanReject = (await AuthorizationService.AuthorizeAsync(User, Contact, ContactOperations.Reject)).Succeeded;
            CanEdit = (await AuthorizationService.AuthorizeAsync(User, Contact, ContactOperations.Update)).Succeeded;

            // Ensure the user has permission to view the contact
            if (!(User.IsInRole(Constants.ContactManagersRole) || User.IsInRole(Constants.ContactAdministratorsRole))
                && currentUserId != Contact.OwnerID
                && Contact.Status != ContactStatus.Approved)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, string statusString)
        {
            // Validate and parse the status string
            if (!Enum.TryParse(statusString, out ContactStatus status))
            {
                ModelState.AddModelError(string.Empty, "Invalid status.");
                return Page(); // Return to the page to show the error
            }

            // Fetch the contact
            var contact = await Context.Contact.FirstOrDefaultAsync(m => m.ContactId == id);

            if (contact == null)
            {
                return NotFound();
            }

            // Determine the operation to authorize
            var contactOperation = (status == ContactStatus.Approved)
                                    ? ContactOperations.Approve
                                    : ContactOperations.Reject;

            // Check if the user is authorized to perform the operation
            var isAuthorized = await AuthorizationService.AuthorizeAsync(User, contact, contactOperation);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            // Update the contact status
            contact.Status = status;
            Context.Contact.Update(contact);

            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception and show an error message
                return StatusCode(500, "An error occurred while updating the contact.");
            }

            return RedirectToPage("./Index");
        }
    }
}
