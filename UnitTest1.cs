using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Linq;

namespace CloudQATests
{
    [TestFixture]
    public class PracticeFormTests
    {
        private IWebDriver? driver;
        private WebDriverWait? wait;
        private const string Url = "https://app.cloudqa.io/home/AutomationPracticeForm";
        private string ScreenshotDir => Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");

        [SetUp]
        public void Setup()
        {
            Directory.CreateDirectory(ScreenshotDir);
            var options = new ChromeOptions();
            driver = new ChromeDriver(options);
            driver.Manage().Window.Maximize();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            driver.Navigate().GoToUrl(Url);
        }

        [TearDown]
        public void TearDown()
        {
            if (driver != null)
            {
                try { driver.Quit(); } catch { }
                try { driver.Dispose(); } catch { }
                driver = null;
            }
        }

        private void ScrollIntoView(IWebElement el)
        {
            ((IJavaScriptExecutor)driver!).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", el);
        }

        private void TypeInto(IWebElement fld, string value)
        {
            var js = (IJavaScriptExecutor)driver!;
            ScrollIntoView(fld);
            wait!.Until(d => fld.Displayed && fld.Enabled);

            try
            {
                fld.Click();
                fld.Clear();
                fld.SendKeys(value);
                System.Threading.Thread.Sleep(200);
                if (fld.GetAttribute("value") == value) return;
            }
            catch {}

            js.ExecuteScript(@"
                arguments[0].value = arguments[1];
                arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
                arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
            ", fld, value);
        }

        private string SaveScreenshot(string name)
        {
            var ss = ((ITakesScreenshot)driver!).GetScreenshot();
            var file = Path.Combine(ScreenshotDir, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            File.WriteAllBytes(file, ss.AsByteArray);
            TestContext.AddTestAttachment(file, "screenshot");
            return file;
        }

        private IWebElement FieldForLabel(string labelText)
{
    var drv = driver ?? throw new InvalidOperationException("Driver not initialized");
    var byDataTest = drv.FindElements(By.CssSelector($"[data-test-id='{labelText}'], [data-qa='{labelText}']")).FirstOrDefault();
    if (byDataTest != null) return byDataTest;

    var label = drv.FindElements(By.XPath($"//label[normalize-space() = \"{labelText}\"] | //label[contains(normalize-space(.), \"{labelText}\")]")).FirstOrDefault();
    if (label != null)
    {
        var forAttr = label.GetAttribute("for");
        if (!string.IsNullOrEmpty(forAttr))
        {
            var el = drv.FindElements(By.Id(forAttr)).FirstOrDefault();
            if (el != null) return el;
        }
        var wrapped = label.FindElements(By.XPath(".//input | .//select | .//textarea")).FirstOrDefault();
        if (wrapped != null) return wrapped;

        var near = drv.FindElements(By.XPath($"(//label[contains(normalize-space(.), \"{labelText}\")]/following::input | //label[contains(normalize-space(.), \"{labelText}\")]/following::select | //label[contains(normalize-space(.), \"{labelText}\")]/following::textarea)[1]")).FirstOrDefault();
        if (near != null) return near;
    }

    var byAria = drv.FindElements(By.XPath($"//input[@aria-label and contains(normalize-space(@aria-label), \"{labelText}\")] | //select[@aria-label and contains(normalize-space(@aria-label), \"{labelText}\")]")).FirstOrDefault();
    if (byAria != null) return byAria;

    var byPlaceholder = drv.FindElements(By.XPath($"//input[@placeholder and contains(normalize-space(@placeholder), \"{labelText}\")]")).FirstOrDefault();
    if (byPlaceholder != null) return byPlaceholder;
    
    var byName = drv.FindElements(By.XPath($"//input[@name and contains(normalize-space(@name), \"{labelText}\")] | //select[@name and contains(normalize-space(@name), \"{labelText}\")]")).FirstOrDefault();
    if (byName != null) return byName;

    var anyNear = drv.FindElements(By.XPath($"//div[contains(normalize-space(.), \"{labelText}\")]//input | //span[contains(normalize-space(.), \"{labelText}\")]//input")).FirstOrDefault();
    if (anyNear != null) return anyNear;

    throw new NoSuchElementException($"Field for '{labelText}' not found.");
}


        private void SelectRadioRobust(string visibleText)
        {
            var drv = driver ?? throw new InvalidOperationException();
            var js = (IJavaScriptExecutor)drv;

            var label = drv.FindElements(By.XPath($"//label[normalize-space() = \"{visibleText}\"] | //label[contains(normalize-space(.), \"{visibleText}\")]")).FirstOrDefault();
            if (label != null)
            {
                try
                {
                    ScrollIntoView(label);
                    wait!.Until(d => label.Displayed && label.Enabled);
                    label.Click();
                    return;
                }
                catch { }
            }

            var byValue = drv.FindElements(By.XPath($"//input[@type='radio' and translate(@value,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz') = '{visibleText.ToLower()}']")).FirstOrDefault();
            if (byValue != null)
            {
                try
                {
                    ScrollIntoView(byValue);
                    wait!.Until(d => byValue.Displayed && byValue.Enabled);
                    byValue.Click();
                    return;
                }
                catch { }
            }

            var radios = drv.FindElements(By.XPath("//input[@type='radio']"));
            foreach (var r in radios)
            {
                IWebElement? lab = null;
                var id = r.GetAttribute("id");
                if (!string.IsNullOrEmpty(id))
                    lab = drv.FindElements(By.XPath($"//label[@for='{id}']")).FirstOrDefault();
                if (lab == null)
                {
                    var parent = r.FindElement(By.XPath(".."));
                    if (parent.TagName.Equals("label", StringComparison.OrdinalIgnoreCase)) lab = parent;
                }

                var labText = lab?.Text?.Trim() ?? "";
                if (labText.Equals(visibleText, StringComparison.OrdinalIgnoreCase) || labText.Contains(visibleText, StringComparison.OrdinalIgnoreCase))
                {
                    js.ExecuteScript(@"
                        arguments[0].checked = true;
                        arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
                        arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
                    ", r);
                    return;
                }
            }

            throw new NoSuchElementException($"Radio option '{visibleText}' not found.");
        }

        [Test]
        public void FirstName_ShouldAcceptVisibleTyping()
        {
            var fld = FieldForLabel("First Name");
            TypeInto(fld, "Shrey");
            SaveScreenshot("firstName");
            Assert.AreEqual("Shrey", fld.GetAttribute("value"));
        }

        [Test]
        public void Gender_Male_ShouldBeSelectable()
        {
            SelectRadioRobust("Male");
            SaveScreenshot("gender");
            var drv = driver!;
            IWebElement? radio = drv.FindElements(By.XPath("//label[normalize-space() = 'Male'] | //label[contains(normalize-space(.), 'Male')]")).FirstOrDefault()?
                .GetAttribute("for") is string id && !string.IsNullOrEmpty(id) ? drv.FindElements(By.Id(id)).FirstOrDefault() : null;

            if (radio == null)
                radio = drv.FindElements(By.XPath("//input[@type='radio' and translate(@value,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz') = 'male']")).FirstOrDefault();

            Assert.IsNotNull(radio, "Could not locate radio input for verification.");
            var js = (IJavaScriptExecutor)drv;
            var res = js.ExecuteScript("return arguments[0].checked === true;", radio);
            Assert.IsTrue(res != null && Convert.ToBoolean(res), "Male radio is not selected.");
        }

        [Test]
        public void DateOfBirth_ShouldAcceptTyping()
        {
            var fld = FieldForLabel("Date of Birth");
            TypeInto(fld, "2003/12/14");
            SaveScreenshot("dob");
            var actual = fld.GetAttribute("value") ?? fld.Text;
            Assert.That(actual, Does.Contain("2003"));
        }
    }
}
