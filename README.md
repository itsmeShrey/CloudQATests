A small NUnit + Selenium WebDriver test project that exercises three fields on the CloudQA AutomationPracticeForm:

First Name — visible typing (with JS fallback)

Gender (Male) — robust radio selection (label / value / JS fallback)

Date of Birth — visible typing (with JS fallback)

Steps to run the tests:
# clone the repo
git clone [https://github.com/itsmeShrey/CloudQATests.git](https://github.com/itsmeShrey/CloudQATests)

cd CloudQATests

# restore packages
dotnet restore

# run tests
dotnet test
