C# Coding Standards
====================

Introduction
----------------
The coding standard will be used in conjunction with customized version of *StyleCop* and *FxCop* [**TODO**] during both development and build process. This will help ensure that the standard is followed by all developers on the team in a consistent manner.
  
>"Any fool can write code that a computer can understand. Good programmers write code that humans understand".
>
> Martin Fowler. *Refactoring: Improving the design of existing code.*

### Purpose

The aim of this section is to define a set of C# coding standards to be used by CAPS build team to guarantee maximum legibility, reliability, re-usability and homogeneity of our code. Each section is marked *Mandatory* or *Recommended*. *Mandatory* sections, will be enforced during code reviews as well as tools like *StyleCop* and *FxCop*, and code will not be considered complete until it is compliant.

### Scope

This section contains general C# coding standards which can be applied to any type of application developed in C#, based on *[Framework Design Guidelines](http://msdn.microsoft.com/en-us/library/ms229042.aspx)*. 

It does not pretend to be a tutorial on C#. It only includes a set of limitations and recommendations focused on clarifying the development.

Tools
----------------

* [Resharper](http://www.jetbrains.com/resharper/) is a great 3rd party code cleanup and style tool.
* [StyleCop](http://stylecop.codeplex.com/) analyzes C# srouce code to enforce a set of style and consistency rules and has been integrated into many 3rd party development tools such as Resharper.
* [FxCop](http://codebox/SDLFxCop) is an application that analyzes managed code assemblies (code that targets the .NET Framework common language runtime) and reports information about the assemblies, such as possible design, localization, performance, and security improvements.
* [C# Stylizer](http://toolbox/22561) does many of the style rules automatically

Highlights of Coding Standards
------------------------------

This section is not intended to give a summary of all the coding standards that enabled by our customized StyleCop, but to give a highlight of some rules one will possibly meet in daily coding life. It also provides some recommended however not mandatory(which means not enabled in StyleCop) coding standards.

### File Layout (Recommended)
Only one public class is allowed per file.

The file name is derived from the class name.

		Class   : Observer
		Filename: Observer.cs

### Class Definition Order (Mandatory)

The class definition contains class members in the following order, from *less* restricted scope (public) to *more* restrictive (private):

* Nested types, e.g. classes, enum, struct, etc.
* Field members, e.g. member variables, const, etc.
* Member functions
	* Constructors
	* Finalizer (Do not use unless absolutely necessary)
	* Methods (Properties, Events, Operations, Overridables, Static)
	* Private nested types

### Naming (Mandatory)
* **DO** use PascalCasing for all public member, type, and namespace names consisting of multiple words.

		PropertyDescriptor
		HtmlTag
		IOStream
**NOTE**: A special case is made for two-letter acronyms in which both letters are capitalized, e.g. *IOStream*
* **DO** use camelCasing for parameter names.

		propertyDescriptor
		htmlTag
		ioStream

* **DO** start with underscore for private fields

		private readonly Guid _userId = Guid.NewGuid();

* **DO** start static readonly fields, constants with capitalized case
		
		private static readonly IEntityAccessor EntityAccessor = null;
        private const string MetadataName = "MetadataName";

* **DO NOT** capitalize each word in so-called [closed-form compound words](http://msdn.microsoft.com/en-us/library/ms229043.aspx).

* **DO** have **"Async"** explicitly in the Async method name to notice people how to use it properly

### Formatting (Mandatory)
* **DO** use spaces over tabs, and always show all spaces/tabs in IDE

> **Tips**
> 
> Visual Studio > TOOLS > Options > Text Editor > C# > Tabs > Insert spaces (Tab size: 4)
> 
> Visual Studio > Edit > Advanced > View White Space

* **DO** add *using* inside *namespace* declaration

		namespace Microsoft.Content.Build.BuildWorker.UnitTest
		{
			using System;
		}

* **DO** add a space when:
	1. `for (var i = 0; i < 1; i++)`
	2. `if (a == b)`

### Cross-platform coding
Our code should supports multiple operating systems. Don't assume we only run (and develop) on Windows. Code should be sensitvie to the differences between OS's. Here are some specifics to consider.

* **DO** use `Enviroment.NewLine` instead of hard-coding the line break instead of `\r\n`, as Windows uses `\r\n` and OSX/Linux uses `\n`.

> **Note**
> 
> Be aware that thes line-endings may cause problems in code when using `@""` text blocks with line breaks.

* **DO** Use `Path.Combine()` or `Path.DirectorySeparatorChar` to separate directories. If this is not possible (such as in scripting), use a forward slash `/`. Windows is more forgiving than Linux in this regard.

### Unit tests and functional tests
#### Assembly naming
The unit tests for the `Microsoft.Foo` assembly live in the `Microsoft.Foo.Tests` assembly.

The functional tests for the `Microsoft.Foo` assmebly live in the `Microsoft.Foo.FunctionalTests` assmebly.

In general there should be exactly one unit test assebmly for each product runtime assembly. In general there should be one functional test assembly per repo. Exceptions can be made for both.

#### Unit test class naming
Test class names end with `Test` and live in the same namespace as the class being tested. For example, the unit tests for the `Microsoft.Foo.Boo` class would be in a `Microsoft.Foo.Boo` class in the test assembly.

#### Unit test method naming
Unit test method names must be descriptive about *what is being tested, under what conditions, and what the expectations are*. Pascal casing and underscores can be used to improve readability. The following test names are correct:

```cs
PublicApiArgumentsShouldHaveNotNullAnnotation
Public_api_arguments_should_have_not_null_annotation
```

The following test names are incorrect:

```cs
Test1
Constructor
FormatString
GetData
```

#### Unit test structure
The contents of every unit test should be split into three distinct stages, optionally separated by these comments:

```cs
// Arrange
// Act
// Assert
```

The crucial thing here is the `Act` stage is exactly one statement. That one statement is nothing more than a call to the one method that you are trying to test. keeping that one statement as simple as possible is also very important. For example, this is not ideal:

```cs
int result = myObj.CallSomeMethod(GetComplexParam1(), GetComplexParam2(), GetComplexParam3());
```

This style is not recomended because way too many things can go wrong in this one statement. All the `GetComplexParamN()` calls can throw for a variety of reasons unrelated to the test itself. It is thus unclear to someone running into a problem why the failure occured.

The ideal pattern is to move the complex parameter building into the `Arrange section:

```cs
// Arrange
P1 p1 = GetComplexParam1();
P2 p2 = GetComplexParam2();
P3 p3 = GetComplexParam3();

// Act
int result = myObj.CallSomeMethod(p1, p2, p3);

// Assert
Assert.AreEqual(1234, result);
```

Now the only reason the line with `CallSomeMethod()` can fail is if the method itself blew up.

#### Testing exception messages

In general testing the specific exception message in a unit test is important. This ensures that the exact desired exception is what is being tested rather than a different exception of the same type. In order to verify the exact exception it is important to verify the message.

```cs
var ex = Assert.Throws<InvalidOperationException>(
    () => fruitBasket.GetBananaById(1234));
Assert.Equal(
    "1234",
    ex.Message);
```

#### Use xUnit.net's plethora of built-in assertions
xUnit.net includes many kinds of assertions – please use the most appropriate one for your test. This will make the tests a lot more readable and also allow the test runner report the best possible errors (whether it's local or the CI machine). For example, these are bad:

```cs
Assert.Equal(true, someBool);

Assert.True("abc123" == someString);

Assert.True(list1.Length == list2.Length);

for (int i = 0; i < list1.Length; i++) {
    Assert.True(
        String.Equals
            list1[i],
            list2[i],
            StringComparison.OrdinalIgnoreCase));
}
```

These are good:

```cs
Assert.True(someBool);

Assert.Equal("abc123", someString);

// built-in collection assertions!
Assert.Equal(list1, list2, StringComparer.OrdinalIgnoreCase);
```

#### Parallel tests
By default all unit test assemblies should run in parallel mode, which is the default. Unit tests shouldn't depend on any shared state, and so should generally be runnable in parallel. If the tests fail in parallel, the first thing to do is to figure out why; do not just disable parallel tests!

For functional tests it is reasonable to disable parallel tests.