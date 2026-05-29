# Data Folder

SkyRoof keeps all of its data in the **data folder**.

- Click on **Help / Data Folder** in the main menu to open this folder in File Explorer.
- To open the folder when the program is not running, type this in File Explorer:

    ```bash
    %appdata%\Afreet\Products\SkyRoof`
    ```

## Data Files

- **Settings.json** - this is the file where all user-defined settings are stored;

- **amsat_sat_names.json** - satellite names used on
    [AMSAT Live OSCAR Satellite Status Page](https://www.amsat.org/status/). The
    [Frequency Scale](frequency_scale.md) section explains how to post your observations
    to this page;

- **lotw_sat_names.json** - the list of satellite abbreviations accepted by
    [LoTW](https://www.arrl.org/quick-start);

- **Satellites.json** - the satellite database compiled from the downloaded data;

- **cat_info.json** - lists the CAT capabilities of a generic simplex radio;

- **wsjtx_wisdom.dat** - optimal FFT transform settings found by automatic testing.

## Folders

- **Logs** - contains the log files with error messages and other information;
- **Adif** - QSO records stored in the ADIF format;
- **Downloads** - a copy of the satellite data downloaded from various sources, kept for troubleshooting;
- **Palettes** - definition of the color palettes used by the waterfall display. Add your own
    palette as a text file with "html" color codes. Pick the color codes at
    [htmlcolorcodes.com](https://htmlcolorcodes.com/);
- **sat_images** - cached satellite thumbnails shown by the
    [Satellite Photo](satellite_photo.md) widget. Delete this folder to force re-download.
