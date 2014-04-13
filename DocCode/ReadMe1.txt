To debug into the client.

	1) Simple start debugging into any unit test. The server will start automatically.
	
	Note that once the server has started you will be able to either run or debug your tests without worrying about restarting the server.

To debug into the server.

   1) go into "Project Properties" on the DocCode.Server project and change 'Always start when debugging' from 'True' to 'False'
   2) Save the solution and open a 2nd instance of Visual Studio and debug directly into the DocCode.Server project. Add your  server breakpoints here.
   3) Go back to original solution and debug (or run) any unit test.