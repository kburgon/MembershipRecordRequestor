# MembershipRecordRequestor

The Membership Record Requestor is a tool to help make requesting membership records as a ward clerk on [lds.org/lcr](https://lds.org/lcr) faster.  The user is presented with a WPF window that lets them enter in the personal information of the individual they want to request records for, and when the user finishes entering in the information it is added to a queue of records to request.  A background thread is started that checks for membership information in the queue, the background process navigates uses lds.org/lcr to request the records of the individual that is listed.  Any errors are logged to a text file so that failures can be addressed later.

This application is primarily made using a Windows Presentation Format GUI application for the UI, and the background work is done using a Selenium Webdriver running on Chrome (a Chromedriver).  The logs are written using a simple text logger that I wrote.
