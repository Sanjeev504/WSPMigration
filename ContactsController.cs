using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WSPMigration.Models;
using WSPMigration.Services;

namespace WSPMigration.Controllers
{
    /// <summary>
    /// Controller for Contacts management
    /// Maps to WSP page: Contacts.wsp
    /// - Page title: "Contacts Add/Edit"
    /// - Purpose: Draws the contacts tab for the policy center on the ASC
    /// 
    /// Replaced functionality:
    /// - WSP LISTSQL=@GETSECTION -> GetContactsList() endpoint
    /// - WSP DELETEsp=up_contacts_deleteContact -> DeleteContact() endpoint
    /// - WSP REDIRECT_SECTION validation -> Built into service layer
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactsController(IContactService contactService)
        {
            _contactService = contactService;
        }

        /// <summary>
        /// Gets list of contacts for the policy center
        /// 
        /// Maps to WSP configuration:
        /// - LISTSQL=@GETSECTION - retrieve contacts via GETSECTION
        /// - SUBLIST=CONTACTSLIST - contacts appear in a sublist
        /// - SEARCHFIELDS=Y - search is enabled
        /// - LISTBREAKLIST_DESCRIP - list broken by description
        /// - LISTBREAKCOLUMNS=2 - two columns in break
        /// - LISTONLY=N - not list-only display
        /// - LISTCOUNT=N - no count displayed
        /// - LISTINDENT=100% - full indentation
        /// 
        /// Input mapping (WSP variables):
        /// - $USERID -> UserId
        /// - $SESSIONID -> SessionId (quoted in LAC-53366-003)
        /// - $AGENCY -> Agency
        /// 
        /// History references:
        /// - HMI-C10880: Removed required contacts notice
        /// - HMI-C11072: Updated to dtp contacts table
        /// - MSAEC-11228: Changed to look at enabled flag for editability
        /// - PCO-C26877: Removed hyperlinks for Producer/CSR/Accounting; replaced CnEdit with canChange
        /// - MR-C35497-004: Increased CREATEDBY/MODIFIEDBY to integer user id
        /// - MAG-C43669: Changed stored procedure to up_contacts_tablist
        /// </summary>
        [HttpPost("list")]
        public async Task<ActionResult<ContactListResponse>> GetContactsList([FromBody] ContactListRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null");
            }

            // Validate required fields (maps to WSP PARAMETERS=D)
            if (request.UserId <= 0 || string.IsNullOrEmpty(request.SessionId) || string.IsNullOrEmpty(request.Agency))
            {
                return BadRequest("UserId, SessionId, and Agency are required");
            }

            var response = await _contactService.GetContactsListAsync(request);

            if (!response.IsValid)
            {
                return Unauthorized(response.Message);
            }

            return Ok(response);
        }

        /// <summary>
        /// Gets a single contact by ID
        /// Supports the contacts search functionality (SEARCHFIELDS=Y)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContactDto>> GetContact(
            int id,
            [FromQuery] int userId,
            [FromQuery] string sessionId,
            [FromQuery] string agency)
        {
            if (userId <= 0 || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(agency))
            {
                return BadRequest("UserId, SessionId, and Agency are required");
            }

            // Validate request
            var validation = await _contactService.ValidateRequestAsync(userId, sessionId, agency);
            if (!validation.IsValid)
            {
                return Unauthorized(validation.Message);
            }

            var request = new ContactListRequest
            {
                UserId = userId,
                SessionId = sessionId,
                Agency = agency,
                SearchText = id.ToString()
            };

            var response = await _contactService.GetContactsListAsync(request);

            if (!response.IsValid || response.Contacts.Count == 0)
            {
                return NotFound();
            }

            return Ok(response.Contacts[0]);
        }

        /// <summary>
        /// Deletes a contact record
        /// 
        /// Maps to WSP configuration:
        /// - DELETEsp=up_contacts_deleteContact - delete operation
        /// - REDIRECT=GETSECTION - redirect after delete
        /// 
        /// Security:
        /// - Validates request via REDIRECT_SECTION logic
        /// - Checks user permissions (canChange flag)
        /// - Validates session and agency context
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteContact(
            int id,
            [FromQuery] int userId,
            [FromQuery] string sessionId,
            [FromQuery] string agency)
        {
            if (userId <= 0 || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(agency))
            {
                return BadRequest("UserId, SessionId, and Agency are required");
            }

            var request = new ContactDeleteRequest
            {
                ContactId = id,
                UserId = userId,
                SessionId = sessionId,
                Agency = agency
            };

            bool success = await _contactService.DeleteContactAsync(request);

            if (!success)
            {
                return StatusCode(500, "Error deleting contact");
            }

            // After delete, would redirect to GETSECTION in WSP
            // In REST API, just return success
            return NoContent();
        }

        /// <summary>
        /// Validates a user request
        /// 
        /// Maps to WSP: REDIRECT_SECTION
        /// Executes: exec up_asc_isValidRequest $USERID, '$SESSIONID', '$AGENCY', '', @ret output, @message output
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<object>> ValidateRequest(
            [FromQuery] int userId,
            [FromQuery] string sessionId,
            [FromQuery] string agency)
        {
            if (userId <= 0 || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(agency))
            {
                return BadRequest("UserId, SessionId, and Agency are required");
            }

            var (isValid, message) = await _contactService.ValidateRequestAsync(userId, sessionId, agency);

            return Ok(new
            {
                isValid = isValid,
                message = message
            });
        }

        /// <summary>
        /// Gets page metadata (replaces WSP [SPECS] section)
        /// Returns configuration that would have been in WSP SPECS section
        /// </summary>
        [HttpGet("metadata")]
        public ActionResult<object> GetMetadata()
        {
            return Ok(new
            {
                title = "Contacts Add/Edit",
                system = "ASC",
                deployTarget = "ASC ONLY",
                description = "Draws the contacts tab for the policy center on the ASC",
                configuration = new
                {
                    banner = false,
                    box = false,
                    nodim = false,
                    nosave = true,
                    moreset = true,
                    parameters = "D",
                    debug = false,
                    useMap = false,
                    searchFields = true,
                    listOnly = false,
                    listCount = false,
                    listIndent = "100%",
                    listBreakColumns = 2
                },
                storedProcedures = new
                {
                    listData = "up_contacts_tablist",
                    deleteContact = "up_contacts_deleteContact",
                    validateRequest = "up_asc_isValidRequest"
                },
                mappings = new
                {
                    listsql = "@GETSECTION",
                    sublist = "CONTACTSLIST",
                    redirect = "GETSECTION"
                }
            });
        }
    }
}
