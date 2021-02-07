/*
 * Don't forget!
 * 1) Currently the download all method won't properly update the progressbar to 100% if it isn't handling 100% of the patent list
 * 2) Make absolutely sure that there's no funny business with resuming a download - e.g. skip a file in the middle
 * 
 * 
 * Ongoing:
 * 1) Make sure the download url generation works for foreign patents as well
 *      a) Could necessitate changes to the way an initial input string is parsed
 * 2) Consider adding user-selected download paths instead of targeting the same directory as the input source
 * 3) Develop error logging system to generate logs for me in case of errors
 * 4) Add support for multiple files at a later point
 *      a) This probably calls for some changes around the idea of the "open file" being the last one selected
 * 
 */