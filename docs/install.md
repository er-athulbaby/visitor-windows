# Installing Service Gate Pass

## Install on a new PC
1. Plug in the CPR card reader.
2. Run `ServiceGatePass-Setup-<version>.exe` as an administrator. If the card reader service is
   missing or not running, Setup shows **Install the CPR card reader service**, ticked by default.
   Leave it ticked. Setup then follows the vendor's steps:
   - It runs the vendor's `SCardReadServer.exe` setup. Click through its screens.
   - It stops the "CIO GCC CardRead Server" service.
   - It copies the new Bahrain SDK files over the old ones. The files are written by the installer,
     so there is no "Unblock" step.
   - It sets the service to start automatically and starts it.
   If a broken service from an earlier attempt exists, Setup removes it first. The option is hidden
   when the service is already running.
3. On first start, fill in the hotel name, logo, locations and the first admin account.
4. Check: with a card inserted, **Read CPR card** (F2) fills the form.

Uninstalling Service Gate Pass leaves the card reader service in place, because other software may
use it. To remove it, use the vendor's `Uninstall.exe` in its folder.

### Card reader service by hand (only if you unticked the option)
1. Copy the vendor's `SCardReadServer` folder to the PC and run `SCardReadServer.exe`.
2. For new Bahrain cards: stop the service, replace its SDK files with the ones from
   `Bahrain Smart Card SDK-V1.1.rar`, then start the service again.
3. If a "CAS policy" `NotSupportedException` appears, open
   `Bahrain Smart Card SDK\DotNet\Extensions\BAH`. For each file: right-click → Properties →
   tick **Unblock** → OK.

## After installing
1. Sign in as the admin.
2. **Settings** → choose a **Backup folder** (a USB drive or network
   share) → **Save**. A copy is made immediately and then every day; the newest 30 are kept.
3. **Users** → add one account per guard (role: Gate staff).

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
From the repo root:
1. Put the vendor files in the git-ignored `SCardReadServer\` folder, which holds `SCardReadServer.exe`
   and `Bahrain Smart Card SDK-V1.1.rar`. Extract the SDK beside them:
   `tar -xf "SCardReadServer\Bahrain Smart Card SDK-V1.1.rar" -C SCardReadServer\BahrainSDK`
   (create `SCardReadServer\BahrainSDK` first; Windows 10/11 `tar` reads `.rar`).
   Check first that CIO GCC allows you to redistribute these files.
2. Publish a self-contained single file, then compile the Inno Setup script:

```
dotnet publish src/ServiceGatePass -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
"<Inno Setup 6>\ISCC.exe" installer/ServiceGatePass.iss
```
