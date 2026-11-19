# Service Gate Pass — Design

Date: 2026-11-19
Status: Awaiting owner review

## 1. Purpose

Replace the paper book at a hotel's **service gate**. Gate staff hold a
service visitor's physical CPR card until they leave and today write name,
location, time and details by hand. The app must make check-in and check-out
clearly faster than the book, and staff must always be able to see whose CPR
cards they are holding.

Guiding rule from the owner: **don't make the system complicated.**

- Users: hotel security / gate staff at the service entrance.
- Visitors: contractors, suppliers, maintenance, delivery. Not hotel guests.
- Sold as a product to several hotels; each hotel is installed separately.

## 2. Decisions

| Topic | Decision |
|---|---|
| Platform | Native Windows desktop app, works fully offline |
| Stack | C# + WPF on .NET 10, SQLite via EF Core, xUnit tests |
| Customers | Product for several hotels; per-hotel name, logo, locations, admin. No licensing |
| PCs per hotel | One gate PC with a local database in v1. Shared database may come later |
| Badge number | Optional field, switched on/off per hotel |
| Card slot number | Optional field, switched on/off per hotel |
| Logins | Per-staff username + password; roles **Admin** and **Gate** |
| Reports | History search, CSV export, print, dashboard |
| PC failure | Staff fall back to the paper book; app makes daily backup copies |
| Data retention | Out of v1 — decide after a customer's legal team weighs in |
| Languages | English and Arabic (Arabic is right-to-left) |

## 3. Screens and flow

### 3.1 Gate screen (Gate and Admin roles)

One window with two panels, always visible.

**Left — Check in**
1. Staff insert the CPR card and press **Read card** (or F2). Name (English
   and Arabic), CPR, employer and card photo appear. The photo is displayed
   only and never stored.
2. If this CPR has visited before, location, company and note pre-fill from
   the previous visit.
3. Staff pick a **location** from large buttons (the hotel's active locations,
   in the configured order).
4. Optional: company (defaults to the card's employer), note, **badge #** (if
   enabled), **slot #** (if enabled; defaults to the lowest free slot).
5. **Check in** (Enter). The form clears for the next visitor.

- If the reader fails, its error is shown in plain words and staff can type
  the name and CPR by hand.
- The CPR must be exactly 9 digits (this catches typos in manual entry).
- If the CPR is already inside, check-in is refused with the current location
  and time ("Already inside at Kitchen since 09:12").

**Right — Inside now**
- Rows: name, location, time in, duration, badge #, slot #.
- The search box filters by name, CPR, badge # or slot #.
- Select a row → **Check out** → hand back the card. Pressing Enter in the
  search box when exactly one row matches checks that row out.
- Counter: "N inside · N cards held".

### 3.2 Admin screens (Admin role only)

- **History** — filter by date range, name/CPR, location, company. Export CSV
  and print.
- **Dashboard** — visits today, inside now, visits per day (last 30 days),
  top locations, top companies. Charts use LiveCharts2.
- **Settings** — hotel name and logo; locations (add, rename, reorder,
  hide); badge # on/off; slot # on/off plus slot count; backup folder; card
  reader URL; language.
- **Users** — add or disable staff, reset passwords, set roles.

### 3.3 First run

When the database has no users, a setup wizard asks for the hotel name, an
optional logo, the starting locations and the first Admin account.
After that, the app opens on a login screen. **Log out** in the header
returns to it so the next shift's guard can sign in.

### 3.4 Language

An English/Arabic switch in the window header. Arabic sets
`FlowDirection=RightToLeft` for the whole window. English text is the key,
as in VisitorPass. Arabic lives in one embedded `Resources/ar.json`, ported
from VisitorPass `lang/ar.json` where it fits. Code calls `L.T("...")` and
XAML uses `{l:T '...'}`. A test fails if any of those strings has no Arabic
entry. (This replaces .resx files, whose generated code doesn't build from
the `dotnet` CLI.)

## 4. Data and storage

Database file: `%ProgramData%\ServiceGatePass\gate.db` (the same for every
Windows user on the PC).

| Table | Columns |
|---|---|
| Visitor | Id, Cpr (unique), NameEn, NameAr, Employer, LastCompany, LastLocationId, LastNote, CreatedAt, UpdatedAt |
| Visit | Id, VisitorId, LocationId, Company, Note, BadgeNo (nullable), SlotNo (nullable), CheckInAt, CheckInByUserId, CheckOutAt (nullable), CheckOutByUserId (nullable) |
| Location | Id, Name, NameAr (nullable), SortOrder, IsActive |
| User | Id, Username (unique), DisplayName, PasswordHash, Role (Admin/Gate), IsActive |
| Setting | Key (PK), Value |

**Rules enforced by the database (partial unique indexes):**
- One open visit per visitor: `UNIQUE(VisitorId) WHERE CheckOutAt IS NULL`.
- One open visit per badge #: `UNIQUE(BadgeNo) WHERE CheckOutAt IS NULL AND BadgeNo IS NOT NULL`.
- One open visit per slot #: `UNIQUE(SlotNo) WHERE CheckOutAt IS NULL AND SlotNo IS NOT NULL`.

**Other rules:**
- "Inside now" means `Visit.CheckOutAt IS NULL`. There is no separate status
  column.
- Locations and users are deactivated, never deleted, so history keeps their
  names.
- Passwords are hashed with PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`, SHA-256,
  random salt).
- Times are stored as local time (Bahrain has one timezone and no daylight
  saving).
- Only CPR, English and Arabic names and employer are taken from the card.
  Photo, address, passport, visa and biometrics are never stored.

**Backup:** on start, and every 24 hours while the app runs, the app copies
the database to the backup folder as `gate-YYYY-MM-DD.db` using SQLite's
online backup. It keeps the newest 30 copies. If a copy fails, a warning
banner shows until the next copy succeeds. If no backup folder is set, a
banner reminds the Admin to set one.

**Upgrades:** on start the app takes a backup, then applies pending EF Core
migrations.

## 5. Card reader

The vendor's "CIO GCC CardRead Server" Windows service must be installed
and running. The app's installer does not install it; installation steps are
documented separately.

`CardReader` sends `POST {ReaderUrl}`, default
`http://localhost:5050/api/operation/ReadCard`, with content type
`text/plain` and this body:

```json
{"ReadCardInfo":false,"ReadPersonalInfo":true,"ReadAddressDetails":false,
 "ReadBiometrics":false,"ReadEmploymentInfo":true,"ReadImmigrationDetails":false,
 "ReadTrafficDetails":false,"SilentReading":false,"ReaderIndex":-1,
 "ReaderName":"","OutputFormat":"JSON","ValidateCard":false}
```

The timeout is 10 seconds. The result is either card data or an error
message:

- A non-empty `ErrorDescription` means failure, even though the HTTP request
  succeeded.
- A refused connection or timeout gives: "Card reader service is not running —
  start CIO GCC CardRead Server."
- Field mapping:
  - CPR: `IdNumber`, falling back to `MiscellaneousTextData.CPRNO`
  - English name: `EnglishFullName`, falling back to first name + last name
  - Arabic name: `ArabicFullName`
  - Employer: the first non-empty of `EmployerName`, `EmploymentNameEnglish`,
    `SponserNameEnglish`
  - Photo: `PhotoB64Encoded` (displayed only)

**Prerequisite before implementing the mapping:** capture one fully
successful read from a real card and save it as a test fixture. This
confirms field names, the date format and the photo encoding.

**Spike (not v1 scope):** WebSocket `ws://localhost:5060/SCardRead` with
`RunCardDetection`, to read automatically when a card is inserted. Adopt it
only if its events prove reliable.

## 6. Errors and logging

- A broken database rule (already inside, badge in use, slot in use) shows a
  clear message next to the form. Nothing is saved.
- Unexpected errors are logged to
  `%ProgramData%\ServiceGatePass\logs\YYYY-MM-DD.log` and shown as a short
  message. The app keeps running.

## 7. Export

CSV is written as UTF-8 with BOM so Excel shows Arabic correctly. Any cell
starting with `=`, `+`, `-` or `@` gets a leading `'` to block formula
injection. Print uses WPF's `PrintDialog` on a simple table of the filtered
history.

## 8. Code layout

```
ServiceGatePass.sln
src/ServiceGatePass/          WPF app
  Data/                       GateDb (DbContext), entities, migrations
  Services/                   CardReader, GateService, BackupService, CsvExport, PasswordHasher
  Views/                      GateView, HistoryView, DashboardView, SettingsView, UsersView, SetupWizard, LoginView
  Resources/                  L.cs (translation lookup + {l:T} markup extension), ar.json
tests/ServiceGatePass.Tests/  xUnit
```

Screens call the services directly. There are no repository or interface
layers.

## 9. Testing

Automated (xUnit):
- `CardReader` mapping, against the captured real response and the
  error-response samples.
- `GateService`, on in-memory SQLite: a duplicate check-in is blocked; badge
  and slot reuse are blocked while a visit is open; check-out frees the badge
  and slot; returning visitors pre-fill from their last visit.
- `CsvExport`: BOM present, formula cells escaped, Arabic preserved.
- `PasswordHasher`: verify round-trip; a wrong password fails.

Manual checklist: the gate flow with a real card reader, the reader-down
message, Arabic right-to-left layout, the first-run wizard, a backup file
created, and the installer on a clean PC.

## 10. Packaging

A self-contained, single-file `win-x64` publish, wrapped in an Inno Setup
installer. The hotel PC needs no separate .NET install.

## 11. Out of scope for v1

- Data retention / automatic deletion (pending customer legal review)
- Several PCs sharing one database
- Licensing
- Email or any other network notifications
- Auto-read when a card is inserted (spike only)
- Audit log beyond the check-in/check-out user fields
- Visitor types, host staff member, escort flags
