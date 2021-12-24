using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Net.Http.Headers;
using MyCourse.Models.Entities;
using MyCourse.Models.Enums;
using MyCourse.Models.InputModels.Users;

namespace MyCourse.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        public UsersModel(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        [BindProperty]
        public UserRoleInputModel Input { get; set; }

        public IList<ApplicationUser> Users { get; private set; }

        [BindProperty(SupportsGet = true)]
        public Role InRole { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Gestione utenti";
            Claim claim = new (ClaimTypes.Role, InRole.ToString());
            Users = await userManager.GetUsersForClaimAsync(claim);
            return Page();
        }

        public async Task<IActionResult> OnPostAssignAsync()
        {
            //Verificare se il modelstate.IsValid è true
            if (!ModelState.IsValid)
            {
                return await OnGetAsync();
            }

            //Con lo userManager recuperiamo l'utente tramite la mail
            ApplicationUser user = await userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(nameof(Input.Email), $"L'indirizzo email: {Input.Email} non corrisponde ad alcun utente!");
                return await OnGetAsync();
            }

            //Con lo userManager recuperiamo i vari claim dell'utente
            IList<Claim> claims = await userManager.GetClaimsAsync(user);

            //Verifichiamo se ha già quel ruolo
            Claim roleClaim = new (ClaimTypes.Role, Input.Role.ToString());

            //se ce l'ha invio messaggio di errore
            if (claims.Any(claim => claim.Type == roleClaim.Type && claim.Value == roleClaim.Value))
            {
                ModelState.AddModelError(nameof(Input.Role), $"Il ruolo: {Input.Role} è già assegnato all'utente: {Input.Email}");
                return await OnGetAsync();
            }

            //Se non ce l'ha assegno il ruolo
            IdentityResult result = await userManager.AddClaimAsync(user, roleClaim);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, $"Operazione è fallita: {result.Errors.FirstOrDefault()?.Description}");
                return await OnGetAsync();
            }
            //Diamo conferma e reindirizziamo
            TempData["ConfirmationMessage"] = $"Il ruolo: {Input.Role} è stato assegnato all'utente: {Input.Email}.";
            return RedirectToPage(new {inrole = (int) InRole});
        }

        public async Task<IActionResult> OnPostRevokeAsync()
        {
            //Verificare se il modelstate.IsValid è true
            if (!ModelState.IsValid)
            {
                return await OnGetAsync();
            }
            //Con lo userManager recuperiamo l'utente tramite la mail
            ApplicationUser user = await userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(nameof(Input.Email), $"L'indirizzo email: {Input.Email} non corrisponde ad alcun utente");
                return await OnGetAsync();
            }
            //Con lo userManager recuperiamo i vari claim dell'utente
            IList<Claim> claims = await userManager.GetClaimsAsync(user);
            //Verifichiamo se non ha  quel ruolo
            Claim roleClaim = new (ClaimTypes.Role, Input.Role.ToString());
            //se non ce l'ha invio messaggio di errore
            if (!claims.Any(claim => claim.Type == roleClaim.Type && claim.Value == roleClaim.Value))
            {
                ModelState.AddModelError(nameof(Input.Role), $"Il ruolo: {Input.Role} non è stato assegnato all'utente: {Input.Email}.");
                return await OnGetAsync();
            }
            //Se ce l'ha revoco il ruolo
            IdentityResult result = await userManager.RemoveClaimAsync(user, roleClaim);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, $"Operazione è fallita: {result.Errors.FirstOrDefault()?.Description}");
                return await OnGetAsync();
            }
            //Diamo conferma e reindirizziamo
            TempData["ConfirmationMessage"] = $"Il ruolo: {Input.Role} è stato rimosso all'utente: {Input.Email}.";
            return RedirectToPage(new {inrole = (int) InRole});
        }
    }
}