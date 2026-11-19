# Installing Service Gate Pass

## 1. Card reader service (once per PC)
1. Copy the `SCardReadServer` folder to the PC.
2. Run `SCardReadServer.exe` — this installs and starts the Windows service "CIO GCC CardRead Server".
3. New Bahrain cards: stop the service, replace its SDK files with the ones from
   `Bahrain Smart Card SDK-V1.1.rar`, start the service again.
4. If a "CAS policy" `NotSupportedException` appears, open
   `Bahrain Smart Card SDK\DotNet\Extensions\BAH`, and for each file:
   right-click → Properties → tick **Unblock** → OK.
5. Check: with a card inserted, the app's **Read card** button fills the form.

## 2. The app
1. Run `ServiceGatePass-Setup-<version>.exe` as an administrator.
2. On first start, fill in the hotel name, logo, locations and the first admin account.
3. Sign in as the admin → **Settings** → choose a **Backup folder** (a USB drive or network
   share) → **Save**. A copy is made immediately and then every day; the newest 30 are kept.
4. **Users** → add one account per guard (role: Gate staff).

## Where data lives
- Database: `C:\ProgramData\ServiceGatePass\gate.db`
- Error logs: `C:\ProgramData\ServiceGatePass\logs\`
- Uninstalling the app does **not** delete the database.

## If the PC fails
Use the paper book. On the replacement PC, install the app, close it, copy the newest
`gate-YYYY-MM-DD.db` from the backup folder to `C:\ProgramData\ServiceGatePass\gate.db`, then start the app.
