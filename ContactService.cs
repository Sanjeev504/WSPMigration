using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WSPMigration.Models;

namespace WSPMigration.Services
{
    /// <summary>
    /// Service layer for Contact operations
    /// Maps to WSP stored procedures and data access:
    /// - up_contacts_deleteContact (WSP: DELETEsp)
    /// - up_contacts_tablist (WSP: LISTSQL=@GETSECTION via CONTACTSLIST_LISTSQL_SECTION)
    /// - up_asc_isValidRequest (WSP: REDIRECT_SECTION validation)
    /// </summary>
    public interface IContactService
    {
        /// <summary>
        /// Gets list of contacts for display
        /// Maps to WSP: LISTSQL=@GETSECTION, SUBLIST=CONTACTSLIST
        /// </summary>
        Task<ContactListResponse> GetContactsListAsync(ContactListRequest request);

        /// <summary>
        /// Deletes a contact record
        /// Maps to WSP: DELETEsp=up_contacts_deleteContact
        /// </summary>
        Task<bool> DeleteContactAsync(ContactDeleteRequest request);

        /// <summary>
        /// Validates user request before processing
        /// Maps to WSP: REDIRECT_SECTION -> up_asc_isValidRequest
        /// </summary>
        Task<(bool IsValid, string Message)> ValidateRequestAsync(int userId, string sessionId, string agency);
    }

    /// <summary>
    /// Implementation of contact service
    /// Replaces WSP engine execution model with standard service pattern
    /// </summary>
    public class ContactService : IContactService
    {
        private readonly string _connectionString;

        public ContactService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Maps to WSP structure:
        /// - Inputs: LISTSQL=@GETSECTION, SEARCHFIELDS=Y, LISTBREAKCOLUMNS=2
        /// - Outputs: CONTACTSLIST with ContactDto records
        /// - Processing: Calls up_contacts_tablist stored procedure
        /// History: MAG-C43669 (04/03/2019) - Changed from CONTACTSLIST_LISTSQL_SECTION to up_contacts_tablist
        /// </summary>
        public async Task<ContactListResponse> GetContactsListAsync(ContactListRequest request)
        {
            var response = new ContactListResponse();

            try
            {
                // Validate request first (maps to WSP REDIRECT_SECTION)
                var validation = await ValidateRequestAsync(request.UserId, request.SessionId, request.Agency);
                if (!validation.IsValid)
                {
                    response.IsValid = false;
                    response.Message = validation.Message;
                    return response;
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("up_contacts_tablist", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Parameters map to WSP input variables
                        command.Parameters.AddWithValue("@UserId", request.UserId);
                        command.Parameters.AddWithValue("@SessionId", request.SessionId);
                        command.Parameters.AddWithValue("@Agency", request.Agency);
                        command.Parameters.AddWithValue("@SearchText", request.SearchText ?? "");
                        command.Parameters.AddWithValue("@BreakListColumn", request.BreakListColumn ?? "");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var contact = new ContactDto
                                {
                                    ContactId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    ContactName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    ContactType = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    IsEnabled = reader.IsDBNull(3) ? false : reader.GetBoolean(3),
                                    CanChange = reader.IsDBNull(4) ? false : reader.GetBoolean(4),
                                    CreatedBy = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                    CreatedDate = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6),
                                    ModifiedBy = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                    ModifiedDate = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8),
                                    Agency = request.Agency
                                };
                                response.Contacts.Add(contact);
                            }
                        }
                    }
                }

                response.IsValid = true;
                response.TotalCount = response.Contacts.Count;
                response.Message = "Contacts retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsValid = false;
                response.Message = $"Error retrieving contacts: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Maps to WSP: DELETEsp=up_contacts_deleteContact
        /// Replaces hyperlink removal logic for Producer, CSR, and Accounting/Billing (PCO-C26877)
        /// Uses canChange flag instead of CnEdit (PCO-C26877)
        /// </summary>
        public async Task<bool> DeleteContactAsync(ContactDeleteRequest request)
        {
            try
            {
                // Validate request first
                var validation = await ValidateRequestAsync(request.UserId, request.SessionId, request.Agency);
                if (!validation.IsValid)
                {
                    return false;
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("up_contacts_deleteContact", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ContactId", request.ContactId);
                        command.Parameters.AddWithValue("@UserId", request.UserId);
                        command.Parameters.AddWithValue("@Agency", request.Agency);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting contact: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Maps to WSP: REDIRECT_SECTION
        /// Calls: exec up_asc_isValidRequest $USERID, '$SESSIONID', '$AGENCY', '', @ret output, @message output
        /// Security enhancement: ADA-C40570 (08/29/2019)
        /// Session ID quotes: LAC-53366-003 (08/10/2023)
        /// </summary>
        public async Task<(bool IsValid, string Message)> ValidateRequestAsync(int userId, string sessionId, string agency)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("up_asc_isValidRequest", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Parameters match WSP REDIRECT_SECTION call
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@SessionId", sessionId);
                        command.Parameters.AddWithValue("@Agency", agency);
                        command.Parameters.AddWithValue("@Flag", "");

                        // Output parameters for return values
                        var retParam = new SqlParameter("@ret", SqlDbType.VarChar, 8000)
                        {
                            Direction = ParameterDirection.Output
                        };
                        var messageParam = new SqlParameter("@message", SqlDbType.VarChar, 8000)
                        {
                            Direction = ParameterDirection.Output
                        };

                        command.Parameters.Add(retParam);
                        command.Parameters.Add(messageParam);

                        await command.ExecuteNonQueryAsync();

                        var ret = retParam.Value?.ToString() ?? "";
                        var message = messageParam.Value?.ToString() ?? "";

                        // Interpret return value (typically "0" for success)
                        bool isValid = string.IsNullOrEmpty(ret) || ret == "0";
                        return (isValid, message);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Validation error: {ex.Message}");
            }
        }
    }
}
