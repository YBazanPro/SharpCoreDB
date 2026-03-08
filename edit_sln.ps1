$content = Get-Content SharpCoreDB.sln -Raw
$insertAfter = 'EndProject\r\nProject("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "installers", "installers", "{F4B5C6D7-A8E9-4F01-B23C-4D5E6F7A8B9C}")'
$replacement = @'
EndProject

Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "javascript (SharpCoreDB-Client)", "javascript (SharpCoreDB-Client)", "{F3A4B5C6-D7E8-4F9A-B0C1-3D4E5F6A7B8C}"
	ProjectSection(SolutionItems) = preProject
		src\clients\javascript\sharpcoredb-client\CHANGELOG.md = src\clients\javascript\sharpcoredb-client\CHANGELOG.md
		src\clients\javascript\sharpcoredb-client\jest.config.js = src\clients\javascript\sharpcoredb-client\jest.config.js
		src\clients\javascript\sharpcoredb-client\package-lock.json = src\clients\javascript\sharpcoredb-client\package-lock.json
		src\clients\javascript\sharpcoredb-client\package.json = src\clients\javascript\sharpcoredb-client\package.json
		src\clients\javascript\sharpcoredb-client\publish.sh = src\clients\javascript\sharpcoredb-client\publish.sh
		src\clients\javascript\sharpcoredb-client\README.md = src\clients\javascript\sharpcoredb-client\README.md
		src\clients\javascript\sharpcoredb-client\tsconfig.json = src\clients\javascript\sharpcoredb-client\tsconfig.json
		src\clients\javascript\sharpcoredb-client\tsup.config.ts = src\clients\javascript\sharpcoredb-client\tsup.config.ts
		src\clients\javascript\sharpcoredb-client\.github\workflows\publish.yml = src\clients\javascript\sharpcoredb-client\.github\workflows\publish.yml
		src\clients\javascript\sharpcoredb-client\examples\basic-example.js = src\clients\javascript\sharpcoredb-client\examples\basic-example.js
		src\clients\javascript\sharpcoredb-client\examples\pooling-example.js = src\clients\javascript\sharpcoredb-client\examples\pooling-example.js
		src\clients\javascript\sharpcoredb-client\examples\README.md = src\clients\javascript\sharpcoredb-client\examples\README.md
		src\clients\javascript\sharpcoredb-client\src\connection.ts = src\clients\javascript\sharpcoredb-client\src\connection.ts
		src\clients\javascript\sharpcoredb-client\src\errors.ts = src\clients\javascript\sharpcoredb-client\src\errors.ts
		src\clients\javascript\sharpcoredb-client\src\grpc-client.ts = src\clients\javascript\sharpcoredb-client\src\grpc-client.ts
		src\clients\javascript\sharpcoredb-client\src\http-client.ts = src\clients\javascript\sharpcoredb-client\src\http-client.ts
		src\clients\javascript\sharpcoredb-client\src\index.ts = src\clients\javascript\sharpcoredb-client\src\index.ts
		src\clients\javascript\sharpcoredb-client\src\pool.ts = src\clients\javascript\sharpcoredb-client\src\pool.ts
		src\clients\javascript\sharpcoredb-client\src\types.ts = src\clients\javascript\sharpcoredb-client\src\types.ts
		src\clients\javascript\sharpcoredb-client\src\ws-client.ts = src\clients\javascript\sharpcoredb-client\src\ws-client.ts
		src\clients\javascript\sharpcoredb-client\tests\connection.test.ts = src\clients\javascript\sharpcoredb-client\tests\connection.test.ts
		src\clients\javascript\sharpcoredb-client\tests\errors.test.ts = src\clients\javascript\sharpcoredb-client\tests\errors.test.ts
		src\clients\javascript\sharpcoredb-client\tests\pool.test.ts = src\clients\javascript\sharpcoredb-client\tests\pool.test.ts
	EndProjectSection
EndProject

Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "installers", "installers", "{F4B5C6D7-A8E9-4F01-B23C-4D5E6F7A8B9C}"
'@
$content -replace $insertAfter, $replacement | Set-Content SharpCoreDB.sln
