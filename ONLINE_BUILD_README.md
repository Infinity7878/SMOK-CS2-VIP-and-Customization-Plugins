# Build SMOK CS2 plugins online with GitHub Actions

This package includes a GitHub Actions workflow at:

.github/workflows/build-plugins.yml

## Steps

1. Create a new GitHub repository.
2. Upload every file/folder from this zip into the repository.
3. Go to the repository's **Actions** tab.
4. Select **Build SMOK CS2 Plugins**.
5. Click **Run workflow**.
6. When it finishes, open the completed run.
7. Download the artifact named **SMOK-CS2-Plugins-Compiled**.
8. Inside the artifact, upload the plugin folders to your server:

   game/csgo/addons/counterstrikesharp/plugins/SMOKVip/
   game/csgo/addons/counterstrikesharp/plugins/SMOKCustomization/

## Notes

- The workflow uses .NET 8 online, so you do not need .NET installed on your work PC.
- If the build fails, open the failed step in Actions and copy the red error text.
- Upload the full published folder for each plugin, not only the DLL, because dependencies may also be included.
