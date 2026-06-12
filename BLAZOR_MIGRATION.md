# WSP to Blazor Migration Guide

## Project Structure

```
WSPMigration/
├── App.razor                          # Root component (replaces WSP engine routing)
├── Program.cs                         # Startup configuration
├── WSPMigration.csproj                # Project file with dependencies
├── Pages/
│   └── Contacts.razor                 # Main contacts page (maps to Contacts.wsp)
├── Components/
│   └── ContactCard.razor              # Reusable contact display component
├── Layouts/
│   └── MainLayout.razor               # Main layout template
├── Services/
│   ├── IContactService.cs             # Service interface
│   └── ContactService.cs              # Service implementation (SP wrapper)
├── Models/
│   └── ContactDto.cs                  # Data transfer objects
├── Controllers/
│   └── ContactsController.cs          # API endpoints (optional)
└── wwwroot/
    ├── css/
    │   ├── app.css                    # Global styles
    │   └── contacts.css               # Page-specific styles
    └── js/
        └── app.js                     # Client-side JavaScript
```

## WSP to Blazor Mapping

### 1. Page Structure

**WSP: Contacts.wsp**
```
[DESCRIPTION] → Blazor: Page comments, @page directive
[SPECS]       → Blazor: @code block properties, metadata endpoint
[REDIRECT_SECTION] → Blazor: OnInitializedAsync(), service validation
```

**Blazor: Pages/Contacts.razor**
```csharp
@page "/contacts"                    # WSP page routing
@rendermode InteractiveServer        # WSP engine mode
@inject IContactService ContactService  # WSP stored procedure wrapper
```

### 2. Data Flow

**WSP Configuration:**
```
LISTSQL=@GETSECTION              → ContactService.GetContactsListAsync()
SUBLIST=CONTACTSLIST             → ContactCard.razor component
DELETEsp=up_contacts_deleteContact → ContactService.DeleteContactAsync()
REDIRECT_SECTION                 → ContactService.ValidateRequestAsync()
```

**Blazor Flow:**
```
OnInitializedAsync()
  → ValidateAndLoadContacts()
    → ContactService.ValidateRequestAsync()      [REDIRECT_SECTION]
    → ContactService.GetContactsListAsync()      [LISTSQL]
    → Render ContactCard components              [SUBLIST]
    → @foreach displays list                     [LISTBREAKCOLUMNS=2]
```

### 3. Input Variables

| WSP Variable | Blazor Equivalent | Type |
|---|---|---|
| $USERID | userId | int |
| $SESSIONID | sessionId | string |
| $AGENCY | agency | string |

### 4. Configuration Mapping

| WSP SPECS | Blazor Equivalent |
|---|---|
| TITLE=Contacts Add/Edit | Page title, h1 in layout |
| BANNER=N | No banner div in MainLayout |
| BOX=N | No box styling |
| NODIM=N | Standard responsive layout |
| NOSAVE=Y | No form submission (read-only list) |
| MORESET=Y | Reset search button |
| PARAMETERS=D | Parameter validation in code |
| SEARCHFIELDS=Y | Search section component |
| LISTBREAKLIST_DESCRIP | GroupBy in data handling |
| LISTBREAKCOLUMNS=2 | CSS grid-template-columns: repeat(2, 1fr) |
| LISTONLY=N | List is editable (Edit/Delete buttons) |
| LISTCOUNT=N | Count shown but not counted |
| LISTINDENT=100% | Margin-left: 100% on contact items |

### 5. Stored Procedures

| WSP Reference | Service Method | Blazor Usage |
|---|---|---|
| up_contacts_deleteContact | DeleteContactAsync() | Delete button → OnDelete callback |
| up_contacts_tablist | GetContactsListAsync() | Page load, Search action |
| up_asc_isValidRequest | ValidateRequestAsync() | Redirect validation |

### 6. Component Hierarchy

```
MainLayout.razor (navbar, footer, layout)
  └── Contacts.razor (main page)
       └── ContactCard.razor (repeatable component for each contact)
           ├── Contact details display
           └── Action buttons (Edit, Delete)
```

## Key Differences

### WSP Engine vs. Blazor

| Aspect | WSP | Blazor |
|---|---|---|
| **Rendering** | Server-side template engine | Component-based (server or client) |
| **State Management** | Session/Query string | Component @code block |
| **Navigation** | WSP redirect sections | Router/NavigateTo |
| **Validation** | Stored procedures | C# methods + service layer |
| **UI Updates** | Full page reload | Differential updates |
| **Error Handling** | Server-side redirects | JavaScript interop / alerts |

## Usage Examples

### Load Contacts List
```csharp
var request = new ContactListRequest
{
    UserId = userId,
    SessionId = sessionId,
    Agency = agency,
    SearchText = searchText
};

var response = await ContactService.GetContactsListAsync(request);
// Response contains ContactList (WSP: SUBLIST=CONTACTSLIST)
```

### Delete Contact
```csharp
var deleteRequest = new ContactDeleteRequest
{
    ContactId = contact.ContactId,
    UserId = userId,
    SessionId = sessionId,
    Agency = agency
};

bool success = await ContactService.DeleteContactAsync(deleteRequest);
// Then redirect via: await ValidateAndLoadContacts();  [REDIRECT=GETSECTION]
```

### Validate Request
```csharp
var (isValid, message) = await ContactService.ValidateRequestAsync(
    userId, sessionId, agency);

if (!isValid)
{
    // Validation failed (WSP: REDIRECT_SECTION logic)
    validationMessage = message;
}
```

## Running the Application

### Blazor WebAssembly
```bash
dotnet run --configuration Debug
# Launches at https://localhost:5001
```

### Blazor Server
```bash
# Update Program.cs to use ServerProgram
dotnet run
# Launches at https://localhost:5001
```

## Migration Checklist

- [ ] Convert WSP [SPECS] to Blazor @code properties
- [ ] Map WSP stored procedures to IContactService methods
- [ ] Create data transfer objects (DTO) for request/response
- [ ] Build Razor components for UI sections
- [ ] Set up dependency injection in Program.cs
- [ ] Implement error handling and validation
- [ ] Add CSS styling for responsive layout
- [ ] Test with sample data
- [ ] Security validation (maintain WSP permission logic)
- [ ] Performance testing and optimization

## Security Considerations

The Blazor implementation maintains WSP security by:

1. **Validation Before Operations**
   - Calls `up_asc_isValidRequest` before loading/modifying data
   - Validates userId, sessionId, agency context

2. **Permission Checks**
   - `canChange` flag determines Edit/Delete button visibility
   - `IsEnabled` flag indicates read-only contacts

3. **Server-Side Execution**
   - All stored procedures execute server-side
   - No sensitive logic exposed to client

4. **Session Management**
   - SessionId validated on every request
   - Session timeout redirects to login

## Performance Optimization

- **Virtual Scrolling**: For large contact lists (>1000 items)
- **Pagination**: Break contacts into pages
- **Lazy Loading**: Load contact details on-demand
- **Caching**: Cache validation results with expiration
- **CDN**: Serve static assets from CDN

## Future Enhancements

1. **Real-Time Updates**: SignalR for contact changes
2. **Advanced Search**: Filter by contact type, status, date range
3. **Bulk Operations**: Select multiple contacts, bulk delete
4. **Export/Import**: CSV/Excel export of contacts
5. **Audit Trail**: View contact modification history
6. **Mobile Optimization**: Mobile-first responsive design

## Related Files

- WSP Original: [WSP\ExecStoredProc-ASC\pirs\sbjs\StoredProcs\templates\Contacts\Contacts.wsp](Contacts.wsp)
- Service Interface: [Services\IContactService.cs](../ContactService.cs#L1-L50)
- Controller API: [Controllers\ContactsController.cs](../ContactsController.cs)
- Data Models: [Models\ContactDto.cs](../ContactDto.cs)

## Support

For questions about the migration:
- Check Contacts.wsp history comments for context
- Review service method documentation
- Trace stored procedure calls in ContactService.cs
