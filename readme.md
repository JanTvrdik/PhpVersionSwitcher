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


## License

The MIT License (MIT)

Copyright (c) 2014 Jan Tvrdík

Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
