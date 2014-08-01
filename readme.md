# PhpVersionSwitcher

## Installation

1. Download and extract a [release archive](https://github.com/JanTvrdik/PhpVersionSwitcher/releases) to directory of your choice
2. Update `PhpDir` and `HttpServerServiceName` options in `PhpVersionSwitcher.exe.config`
3. Make sure the selected `PhpDir` has the following structure:

```
phpDir/
├── configurations/
│   ├── 5.x.x.ini # php.ini options for all 5.x.x versions
│   ├── 5.3.x.ini # php.ini options for all 5.3.x versions
│   ├── 5.3.7.ini # php.ini options specific for 5.3.7 version
│   └── ...
└── versions/
	├── 5.3.4/
	│   ├── ext/
	│   ├── ...
	│   └── php.exe
	└── 5.6.0-rc3/
		├── ext/
		├── ...
		└── php.exe
```

In all php.ini files you can use `%phpDir%` variable. This is especially useful for `zend_extension`, e.g.

```
zend_extension = "%phpDir%\ext\php_opcache.dll"
zend_extension = "%phpDir%\ext\php_xdebug.dll"
```
