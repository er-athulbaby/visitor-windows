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
- Before an upgrade the app saves `gate-before-upgrade-*.db` in the same folder. These are not deleted
  automatically and can also be used for a restore.
- Uninstalling the app does **not** delete the database.
- Every Windows account on the gate PC can read the database, which holds CPR numbers, so the PC
  should have only trusted accounts.

## If the PC fails
Use the paper book. On the replacement PC:
1. Install the app, start it once, then close it.
2. In `C:\ProgramData\ServiceGatePass`, delete `gate.db`, `gate.db-wal` and `gate.db-shm`.
3. Copy the newest `gate-YYYY-MM-DD.db` from the backup folder into that folder and rename it to `gate.db`.
4. Start the app and check that recent visits appear in **History**.

## Building the installer
From the repo root, publish a self-contained single file, then compile the Inno Setup script:

```
dotnet publish src/ServiceGatePass -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
"<Inno Setup 6>\ISCC.exe" installer/ServiceGatePass.iss
```
