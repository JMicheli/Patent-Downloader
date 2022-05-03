# Changelog

Changes and new versions are documented here.

## [4.0.1] - 2022-05-03

As a release this represents just a hotfix for an issue where unexpected unicode characters that look like spaces could crash the patent number parser. But it is also the first fully tooled version of the application. Thus, under-the-hood the application has changed dramatically.

### Major changes

- Updated patent number parser to remove all non-alphanumeric unicode characters from input strings, preventing unexpected behavior for certain inputs.
