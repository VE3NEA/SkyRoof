# Satellite Photo

The **Satellite Photo** widget on the toolbar shows a thumbnail image of the currently
selected satellite:

![Satellite Photo](../images/satellite_photo.png)

The image is taken from the [SatNOGS](https://db.satnogs.org/) satellite database. Hover
the mouse cursor over the thumbnail to see the satellite name as a tooltip.

The widget is empty when the selected satellite has no image available in the database.

## Image Cache

Downloaded thumbnails are cached on disk so they only need to be fetched once. The cache
is kept in the `sat_images` sub-folder of the [Data Folder](data_folder.md). You may delete
this folder at any time to force SkyRoof to re-download the images.

## Visibility

The widget hides itself automatically when the main window is too narrow to fit all of the
other toolbar widgets. Resize the main window wider to bring it back into view.
