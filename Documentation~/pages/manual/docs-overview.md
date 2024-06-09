# Docs Overview

This article explains the technologies used for our documentation then details how to install and contribute using them.

## How the Docs Website Works

This docs are generated using a tool known as [DocFX](https://github.com/dotnet/docfx). Simply put it generates a website from our codebase (referred to as the scripting API) and articles we have written (such as this very page!).

### Installing DocFX

To install the docfx tool run the following command in a terminal or command prompt window.

```
dotnet tool install -g docfx
```

> [!NOTE]
>
> You may need to [install the .NET SDK](https://dotnet.microsoft.com/en-us/download) if you receive a `Unrecognized command 'dotnet'`  error

> [!NOTE]
>
> Our documentation is generated using `2.75.0`. Using an earlier version may not generate the website correctly. You can check your version using `docfx --version`.
>
> You can update your version with the `dotnet tool update -g docfx` command.

> [!CAUTION]
>
> Do *NOT* install DocFX using a package manager (such as homebrew or chocolatey). These are usually out of date and unmaintained.

### Generate Website

To generate a website from the codebase and articles run the following command from the `Documentation` folder:

```
docfx
```

> [!WARNING]
>
> Getting the `Cannot find config file docfx.json` error? 
> 
> Make sure you change the directory of the terminal window to the project's documentation folder using `cd` 

### Viewing the Website Locally

To view the generated website after it has been generated run the following command from the `Documentation` folder:

```
docfx serve
```

You can now view the website in your browser at `http://localhost:8080`

> [!TIP]
>
> You can generate and serve the website in a single command by using an argument on the command: `docfx --serve`

## Editing Articles

To edit articles head to the `~/Documentation~/pages/*` directory of the package. Find the article you would like to edit in this directory and open it in a text editor of your choice.

These pages are all written in the `markdown` format. We recommend reviewing the [Markdown Syntax](https://www.markdownguide.org/basic-syntax/) page for an overview of the features of markdown. Since we are generating the website using DocFX we also gain access to special [DocFX Specifc Markdown Synax](https://dotnet.github.io/docfx/docs/markdown.html) which can be used in our articles.

> [!TIP]
>
> [Visual Studio Code](https://code.visualstudio.com/Download) is recommended for editing markdown files, but any text editor will do.

## Adding New Articles

To create a new article follow these steps:

1. Create a new markdown file in one of the existing directories at `~/Documentation/pages/*`
	1. These directories correspond to the category the article is in. 
	2. Creating a new category is out of the scope of this article.
2. Within the directory you created the article modify the table of contents (`toc.yml`) file and add a reference to your new article file.
	1. View [DocFX TOC Docs](http://hellosnow.github.io/docfx/tutorial/intro_toc.html) for more information.
3. Optional: Consider adding a link to your article at in the `index.md` of the category and root index.md so this article can more easily be found.

## Adding New Attachments

Want to add images to an article? Simply add the file to the `~/Documentation~/attachments/` directory and it can be referenced in your article as such:

```
![Attachment](../../attachments/manual/docs-overview/example_attachment.png)
```

Or if you want more control over the html properties:

```
<img src="../../attachments/manual/docs-overview/example_attachment.png" width="450">
```

> [!TIP]
>
> All files in the attachments folder can be referenced regardless of it's location.
>
> However, for organizational purposes you are strongly encouraged to place your attachments in a directory that is related to the article it is included in.