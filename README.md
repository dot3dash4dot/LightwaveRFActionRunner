# LightwaveRFActionRunner
A WPF application designed to run over a long period to trigger Lightwave RF Link Plus actions at certain times, optionally depending on whether you are home or not.

# Introduction

This application was written for my specific use case, not as a global solution for everyone. I doubt that it will just work out of the box for anyone (not least due to the home/away implementation - see below for details) but I thought I might as well share it as a starting point for other developers.

# Background

I made this as a replacement for setting up timed automations in the official Lightwave app for two reasons:

#### Home/Away Dependency

Lightwave is great for security purposes - you can automate your lights to turn on and off each evening to make it looks like somebody is home. But it's quite annoying when you _are_ home and your automation suddenly turns off all your lights. This application can check whether or not you are home before triggering actions. *However*, see below as to why this will only currently work for a tiny proportion of users!

#### Smarter Dusk-Dependent Timings

Lightwave supports setting up automations to run at dusk, or even a set time before or after dusk. If you want to run a set of automations over the course of an evening, however, this means you have three choices:
* Set up your events to be relative to dusk (e.g. 4 hours afterwards), in which case your last automation might run at, for example, 11pm in Winter and 2am in Summer.
* Use set times instead of dusk, in which case your lights will be turning on too early or late compared to when it really goes dark
* Use a combination of dusk and set times, in which case sometimes your dusk automation will run first each evening, and sometimes it'll run in the middle of the other automations, depending on the season.

To solve this issue, this application allows you to set up automations that will automatically spread themselves between whenever dusk is that day and a specified "bed time". (The application currently relies on this bed time being before midnight, but this could easily be changed.)

# How it works

#### The Main Window

The application shows a log of the actions in its main window, and this is also logged to a file. The window also shows the last home/away state of each known person and there is a button to refresh these states for testing purposes.

#### Home/Away Dependency

The implementation of this is so specific that it will only currently work for a tiny proportion of people, and I expect that anyone who wants to use this feature will need to write a different implementation.

The application currently works out whether anyone is home or not by working out if their phone is on the home wifi network. Originally I planned to do that by setting up my router to give each phone a reserved IP address that could then be pinged, but iPhones don't respond to ping when they're not active!

Instead, I query my Virgin Media Hub 3 broadband router for a list of devices currently connected to it, and then work out if any match the devices you have specified. You can specify the IP address of the phone if you have set this up to be static in the router, or just match by the phone name.

Getting the list of connected devices is done by calling out to KarlJorgensen's Python scripts (https://github.com/KarlJorgensen/virgin-media-hub3) - thanks obviously to him for his excellent work.

In order to repeat this setup, you'd need to do the following:
* Download a local copy of the virgin-media-hub3 Python scripts and alter LightwaveDaemon's project properties so the xcopy command in the post-build events points at them
* Install Python and ensure the python command is added to Windows' system path variable
* From the command line, navigate to your virgin-media-hub3 folder and run "pip install -r requirements.txt"
* Replace PASSWORD in Device Detector\VirginRouter-GetWifiConnectedDevices.py 

For future reference, other options for a home/away implementation that I considered were:
* Checking if the phone's bluetooth address is visible to the server
* Turning the server into a packet sniffer so you can see if the phone's reserved IP address has been recently active. (Using https://github.com/PcapDotNet/Pcap.Net maybe?)
* Running an app on the phone to detect its location. I believe IFTTT works like this. No idea if there's an app that reports the phone's location to somewhere accessible by other applications, but I suppose you could use the IFTTT app and hack together an IFTTT applet to save the location somewhere.

#### Status Emails

The problem with long-running applications sitting on servers is that it's easy to miss when they aren't behaving as expected. This is especially true if most of your automations are set to not run if you are home - the times you are there to see automations triggered are exactly when they won't trigger!

To combat this, if the application encounters any errors each day, it will send out an email containing these errors after that day's last automation. If there have been no errors, no email is sent, which leads to the next problem: do no emails mean no errors, or that the application isn't running? To solve this, the application will also send out a weekly check-in email to show it is still happily running.

These emails require defining an SMTP server, as discussed below.

#### Setting Up The Application

The application is designed so that most of the configuration data is in one place: Configuration.cs. 

##### Devices

In order to set up a device:

* Provide an enum for the device
* Then in `DeviceRealNameLookup`, provide the name that the Lightwave API returns for each device - an easy way of getting these names is by putting a breakpoint after `GetDevicesInFirstStructureAsync`. This name will be used to trigger the device so must match the API exactly.

##### Automations

You can then set up automations to be run at certain times. Each automation has an "example time" which assumes a dusk time of 6pm and a bed time of 11pm. These will then be converted into real times later depending on the real dusk and bed times.

* First set the real bed time in the `BedTime` property
* Then define your automations:
  * The name is just to allow you to define an easily readable description for the automation and is not used for any other purpose
  * An automation can contain any number of state changes which turn lights on or off
  * `StateChange` has an optional third boolean parameter which defines whether the action should check whether anyone is home before running.
    * By default, this is set to false which means it will only run if no-one is home
    * Provide a true value instead to always run the action regardless of whether anyone is home or not

##### Home/Away Dependency

`Phones` should contain a list of phones that should be checked when determining if anyone is home.

* Person Name is just there as a way of showing whose phone is whose and is not used for any other purpose
* Phone Name is what is looked for when querying the devices connected to the route, unless an IP address is provided for the phone
* If a phone has been given a static IP address, provide it here to match on that rather than the phone's name

##### Status Email Details

* Set the email addresses you want to send emails from/to in `EmailFromAddress` and `EmailToAddress`.
* The SMTP details for your mail server. These can be provided for each call to SendEmail, but it is better to just provide them once in the `if (smtpClient == null)` clause of Utilities.cs\SendEmail

##### Lightwave API Connection Details

You will also need to configure the connection details for the Lightwave API - see the call to the LightwaveAPI constructor in MainWindow.xaml.cs