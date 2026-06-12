namespace WSPMigration.Models
{
    /// <summary>
    /// Data Transfer Object for Contact
    /// Maps from WSP page outputs to API/UI model
    /// </summary>
    public class ContactDto
    {
        /// <summary>
        /// Unique contact identifier
        /// </summary>
        public int ContactId { get; set; }

        /// <summary>
        /// Contact name or description
        /// Maps to LISTSQL result set / CONTACTSLIST sublist
        /// </summary>
        public string ContactName { get; set; }

        /// <summary>
        /// Contact type (e.g., Producer, CSR, Accounting/Billing)
        /// Used for determining editability via enabled flag
        /// </summary>
        public string ContactType { get; set; }

        /// <summary>
        /// Indicates if contact record can be edited
        /// Maps to 'enabled' flag logic from WSP (HMI-C11228)
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Can the current user change this contact?
        /// Replaces WSP 'canChange' logic (PCO-C26877)
        /// </summary>
        public bool CanChange { get; set; }

        /// <summary>
        /// User ID who created the record
        /// Increased to integer (MR-C35497-004)
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Timestamp of record creation
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// User ID who last modified the record
        /// Increased to integer (MR-C35497-004)
        /// </summary>
        public int ModifiedBy { get; set; }

        /// <summary>
        /// Timestamp of last modification
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Agency identifier (from WSP $AGENCY variable)
        /// </summary>
        public string Agency { get; set; }
    }

    /// <summary>
    /// Request model for getting contacts list
    /// Maps to WSP input parameters: $USERID, $SESSIONID, $AGENCY
    /// </summary>
    public class ContactListRequest
    {
        /// <summary>
        /// Current user identifier (WSP: $USERID)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Session identifier (WSP: $SESSIONID)
        /// Enhanced security by adding quotes (LAC-53366-003)
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Agency identifier (WSP: $AGENCY)
        /// </summary>
        public string Agency { get; set; }

        /// <summary>
        /// Search criteria (WSP: SEARCHFIELDS=Y)
        /// </summary>
        public string SearchText { get; set; }

        /// <summary>
        /// Break list column for grouping (WSP: LISTBREAKCOLUMNS=2)
        /// </summary>
        public string BreakListColumn { get; set; }
    }

    /// <summary>
    /// Response model for contacts list
    /// Maps to WSP LISTSQL=@GETSECTION output
    /// </summary>
    public class ContactListResponse
    {
        /// <summary>
        /// List of contacts (WSP: SUBLIST=CONTACTSLIST)
        /// </summary>
        public List<ContactDto> Contacts { get; set; } = new();

        /// <summary>
        /// Total count of contacts (WSP: LISTCOUNT=N means no count displayed)
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Indicates if request was valid
        /// Result of up_asc_isValidRequest validation
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation error message if any
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Request model for deleting a contact
    /// Maps to WSP DELETEsp=up_contacts_deleteContact
    /// </summary>
    public class ContactDeleteRequest
    {
        /// <summary>
        /// Contact ID to delete
        /// </summary>
        public int ContactId { get; set; }

        /// <summary>
        /// User ID performing deletion
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Session ID for validation
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Agency identifier
        /// </summary>
        public string Agency { get; set; }
    }
}
