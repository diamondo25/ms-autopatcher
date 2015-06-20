# ms-autopatcher
MapleStory Autopatcher is a tool that can patch up your MapleStory installation from your version to the last.

## Why not MapleStory's original patcher?

MapleStory's original patcher can only patch those version which are specified in 'Version.info' for the latest version. This means that if MapleStory created patches for version 150, 151 and 152 for version 153, you are out of luck when you've got 149. You'll have to download the whole game again (6GB or more nowadays).

MapleStory Autopatcher will try to make the most efficient patching path from your local version to the latest one. This means that if it detects that you are running on version 149, it'll see which patches do support patches from your version and it will choose the one with the 'biggest step forward'. This could mean that it'll patch your game from 149 to 152, and then from 152 to 153. 
This way you never have to reinstall your game again!

## It takes a while to download, can't you speed it up?

Currently, the application does not support multi-threaded downloading. Maybe in a later version :).

## Your tool broke my MapleStory installation!

Yes, this could happen. When Nexon releases a broken patch (I've seen this happen!), you are screwed. Maybe you want to redownload the game after all?

## Can I remove the v{version number here} folders in my MapleStory directory?

Sure, but you can use them to downgrade. You have to do it from the latest to the oldest one, tho.

## Can I remove the patchfiles that are stored under these folders with the MapleStory locale names (eg. Global)?

Sure, these are put there for manually patching if you need it.

## Why does your tool not support subversions?

Haven't figured out how this works in MapleStory, as they only store the version numbers in Patcher.info
