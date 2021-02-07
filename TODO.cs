/*
 * Don't forget!
 * 1) Currently the download all method won't properly update the progressbar to 100% if it isn't handling 100% of the patent list
 * 
 * 
 * Ongoing:
 * *) Need to get listviews showing data [DONE]
 *      a) Will need to make sure they update properly during download loop
 * 2) Make remaining controls responsive
 *      a) Make sure to wire them up to some sort of state system
 * 3) Figure out File I/O and download requests
 *      *) Will also probably need to parse a webpage to target the download [DONE]
 * *) Disable resizing of the window  [DONE]
 * 5) Make sure the download url generation works for foreign patents as well
 *      a) Could necessitate changes to the way an initial input string is parsed
 * 6) Consider adding user-selected download paths instead of targeting the same directory as the input source
 * *) Change patent list to a dictionary matching data to statuses so that we can do error logging later [DONE]
 * 8) Develop error logging system to generate logs for me in case of errors
 * 9) Add support for multiple files at a later point
 *      a) This probably calls for some changes around the idea of the "open file" being the last one selected
 * 
 * Maybe?
 * 1) Consider re-combining the AppModel and Downloader into just the AppModel but make them partial classes for organization
 *          a) Might be hard because of the callback communication they have now, but this seems like better MVVM
 */