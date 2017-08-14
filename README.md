# TFS Solution History

`View Complete Solution History by a single click`

# The Problem

TFS lacks the functionality to view the history of a solution projects altogether. The exisiting `View History` context menu in Visual Studio will only show those changeset that caused a change to the .sln file like adding or removing a project. 

You have multiple projects spread across different solutions like the following image: 

![image](https://user-images.githubusercontent.com/4930000/29258838-51ad0204-80d2-11e7-9759-dc8e19a9e52c.png)

And during development you modify all the projects. The problem is how to get the full history for a single solution e.g the **Solution 1**?

The following ideas come to the mind:
1.	Right Click the Solution in Solution Explorer and click Source Control and View History
2.	Open Source Control Explorer and get the history for the Projects folder

The first one doesn’t work, since it will give you the history for the .sln and this history only include those changesets that caused a change in .sln file. So we miss most of our changes.

The second one also doesn’t work since it includes the history for all files in the Projects folder which include changes to other solutions (Solution 2 and 3). Again we failed to get the history for solution 1.

# The Solution

We came up with the idea to write a VS extension to fill this gap by enumerating the changes applied to all projects attached to the given solution and combining them altogether. The existing version is not feature complete but it address our mentioned problem.

You can install it in Visual Studio 2015 & 2017.

## Steps to Use:
•	Install the extension
•	Ensure you have a solution opened in VS
•	Click View -> Other Windows -> View Solution History

## Features
•	Author filter: enter full name of author or the user name
•	Double Click to open changeset details in TFS Web access

## Limitations
•	At the moment, the extension fetches only 10 recent history items for any given project.

TFSSolutionHistory will get all the projects history and combine them altogether.

![image](https://user-images.githubusercontent.com/4930000/29249752-febdbc46-804a-11e7-89c7-3ce3b2660e31.png)

# Alternatives

The closest existing solution to the problem is via `Find Changeset` and then filtering the `User` with your own name. Although this will only show your own changes.
