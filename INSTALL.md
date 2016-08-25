# DBBranchManager Installation

## Fetch the Sources

Clone DBBranchManager from [github](https://github.com/aiedail92/DBBranchManager):

```bat
git clone https://github.com/aiedail92/DBBranchManager
```

## Build the Solution

Open the `DBBranchManager.sln` file with Visual Studio and build the solution.

## Setup the Environment

Copy the [dbbm.bat](Tools/dbbm.bat) launcher in a directory listed in your
`PATH`, then edit the first line to adjust the path to your needs:

```bat
mkdir "%USERPROFILE%\usr\local\bin\"
xcopy "C:\Path\To\DBBranchManager\Tools\dbbm.bat" "%USERPROFILE%\usr\local\bin\"
vim "%USERPROFILE%\usr\local\bin\dbbm.bat"
```

If your directory is not in `PATH`, you need to add it:

- Open the *System Properties* window
  - In Windows 10: *Control Panel* > *System and Security* > *System*, then
    click on *Advanced System Settings* on the left
- *Advanced* tab
- Click on the *Environment Variables...* button
- Modify (add if not existing) the `PATH` variable
- Prepend `%USERPROFILE%\usr\local\bin;` (or whatever directory you used) to
  the current value (the **`;`** is not a typo)

Changes are effective on newly opened prompts.

## That's it

You can now start using the tool:

```bat
dbbm help
```
