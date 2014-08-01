# PhpVersionSwitcher

![PhpVersionSwitcher screenshot](http://skladka.merxes.cz/img/phpversionswitcher.png)


## Installation

1. Download and extract a [release archive](https://github.com/JanTvrdik/PhpVersionSwitcher/releases) to directory of your choice

2. Update `PhpDir` and `HttpServerServiceName` options in `PhpVersionSwitcher.exe.config`

3. Make sure the selected `PhpDir` has the following structure:

	```
	%phpDir%/
	├── configurations/
	│   ├── 5.x.x.ini # php.ini options for all 5.x.x versions
	│   ├── 5.3.x.ini # php.ini options for all 5.3.x versions
	│   ├── 5.3.7.ini # php.ini options specific for 5.3.7 version
	│   └── ...
	└── versions/
		├── 5.3.7/
		│   ├── ext/
		│   ├── ...
		│   └── php.exe
		└── 5.6.0-rc3/
			├── ext/
			├── ...
			└── php.exe
	```

4. Create php.ini files in the `configurations` directory. In all php.ini files you can use `%phpDir%` variable. This is especially useful for `zend_extension`, e.g.
	```
	zend_extension = "%phpDir%\ext\php_opcache.dll"
	zend_extension = "%phpDir%\ext\php_xdebug.dll"
	```

5. The active PHP version is always simlinked to `%phpDir%\active`. You may want to add this directory to `PATH`.

6. Update configuration of your HTTP server. The Apache 2.4 configuration related to PHP may look like this:
	```
	LoadModule php5_module "C:/Web/Soft/PHP/active/php5apache2_4.dll"
	PHPIniDir "C:/Web/Soft/PHP/active"
	```

7. Run `PhpVersionSwitcher.exe`.
