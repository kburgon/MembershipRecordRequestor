using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mass_Membership_Record_Requestor.ApplicationObjects
{
    public class RecordRequestor : IDisposable
    {
        public bool ManagerHasRequestAvailable
        {
            get
            {
                requestRunMutex.WaitOne();
                var hasRequestAvailable = m_managerHasRequestsAvailable;
                requestRunMutex.ReleaseMutex();
                return hasRequestAvailable;
            }
            set
            {
                requestRunMutex.WaitOne();
                m_managerHasRequestsAvailable = value;
                requestRunMutex.ReleaseMutex();
            }
        }
        public bool KeepRequestorActive
        {
            get
            {
                requestRunMutex.WaitOne();
                var requestorActive = m_keepRequestorActive;
                requestRunMutex.ReleaseMutex();
                return requestorActive;
            }
            set
            {
                requestRunMutex.WaitOne();
                m_keepRequestorActive = value;
                requestRunMutex.ReleaseMutex();
            }
        }

        private bool m_managerHasRequestsAvailable;
        private bool m_keepRequestorActive;
        private Thread requestRunThread;
        private Mutex requestRunMutex;
        private RequestManager m_requestHost;

        #region Website Contents
        private By membershipMenuLinkLocation;
        private By memberNameSearchBoxLocation;
        private By birthDateInputLocation;
        private By searchErrorLocation;
        private By lookupNameButtonLocation;
        private By householdHeadSelectionLocation;
        private By continueMemberSelectionLocation;
        private By phoneTextBoxLocation;
        private By emailTextBoxLocation;
        private By street1TextBoxLocation;
        private By street2TextBoxLocation;
        private By cityTextBoxLocation;
        private By zipCodeTextBoxLocation;
        private By moveIntoWardButtonLocation;
        private By suggestedRadioButtonLocation;
        private string loginUrl;
        private string moveRecordInUrl;
        private string recordsMovedSuccessMessage;
        private string matchedRecordResultPartialLocation;
        #endregion

        private IWebDriver driver;

        public RecordRequestor()
        {
            requestRunMutex = new Mutex();
            KeepRequestorActive = false;
            ManagerHasRequestAvailable = false;
            m_managerHasRequestsAvailable = false;
            m_keepRequestorActive = true;

            membershipMenuLinkLocation = By.XPath("//*[@translate='menu.membership']");
            memberNameSearchBoxLocation = By.Id("mrnOrName");
            birthDateInputLocation = By.Id("birthDate");
            searchErrorLocation = By.XPath("//*[@ng-if='errors.singleGlobalError']");
            lookupNameButtonLocation = By.XPath("//*[@ng-click='lookup()']");
            householdHeadSelectionLocation = By.Name("newHohMrn");
            continueMemberSelectionLocation = By.XPath("//button[@ng-click='continue()']");
            phoneTextBoxLocation = By.XPath("//*[contains(@ng-model, 'phone')]");
            emailTextBoxLocation = By.Id("email");
            street1TextBoxLocation = By.XPath("(//*[contains(@ng-model, 'address.street1')])[1]");
            street2TextBoxLocation = By.XPath("(//*[contains(@ng-model, 'address.street2')])[1]");
            cityTextBoxLocation = By.XPath("(//*[contains(@ng-model, 'address.city')])[1]");
            zipCodeTextBoxLocation = By.XPath("(//*[contains(@ng-model, 'address.postalCode')])[1]");
            moveIntoWardButtonLocation = By.XPath("//button[@ng-click='move()']");
            suggestedRadioButtonLocation = By.XPath("//input[contains(@value, 'suggested')]");
            loginUrl = "https://lds.org/lcr";
            moveRecordInUrl = "https://www.lds.org/mls/mbr/records/request/find-member";
            recordsMovedSuccessMessage = "The following membership records were successfully";
            matchedRecordResultPartialLocation = "//strong[@class='ng-binding' and contains(text(), '{0}')]";
        }

        public void StartRequestor()
        {
            KeepRequestorActive = true;
            requestRunThread = new Thread(RunRequestSession);
            requestRunThread.Name = "Requestor";
            requestRunThread.Start();
        }

        public void SubscribeToHost(RequestManager hostManager)
        {
            requestRunMutex.WaitOne();
            m_requestHost = hostManager;
            requestRunMutex.ReleaseMutex();
        }

        public void UnsubscribeFromHost()
        {
            requestRunMutex.WaitOne();
            m_requestHost = null;
            requestRunMutex.ReleaseMutex();
        }

        public void RunRequestSession()
        {
            driver = new ChromeDriver();
            var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
            var failedRequestLog = new Logger();

            LoginToWebsite(driver, wait);

            while (KeepRequestorActive)
            {
                if (ManagerHasRequestAvailable)
                {
                    requestRunMutex.WaitOne();
                    var currentRequest = m_requestHost.GetNextRequest();
                    requestRunMutex.ReleaseMutex();

                    try
                    {
                        NavigateToMoveRecordsIn(driver, wait);

                        if (!SearchForRecord(driver, wait, currentRequest))
                        {
                            throw new Exception("Record not found for member.");
                        }

                        EnterMembershipInfo(driver, wait, currentRequest);
                    }
                    catch (Exception e)
                    {
                        failedRequestLog.LogRequestError(e, currentRequest);
                    }
                    finally
                    {
                        NavigateToMoveRecordsIn(driver, wait);
                    }
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }

            driver.Quit();
        }

        private void LoginToWebsite(IWebDriver driver, WebDriverWait wait)
        {
            driver.Navigate().GoToUrl(loginUrl);

            wait.Until(ExpectedConditions.ElementIsVisible(membershipMenuLinkLocation));
        }

        private void NavigateToMoveRecordsIn(IWebDriver driver, WebDriverWait wait)
        {
            driver.Navigate().GoToUrl(moveRecordInUrl);
        }

        private bool SearchForRecord(IWebDriver driver, WebDriverWait wait, MembershipRecordInfo currentRequest)
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(memberNameSearchBoxLocation));
            driver.FindElement(memberNameSearchBoxLocation).Clear();
            driver.FindElement(memberNameSearchBoxLocation).SendKeys(currentRequest.Name);
            driver.FindElement(birthDateInputLocation).Clear();
            driver.FindElement(birthDateInputLocation).SendKeys(currentRequest.BirthDate);
            wait.Until(ExpectedConditions.ElementToBeClickable(lookupNameButtonLocation));
            driver.FindElement(lookupNameButtonLocation).Click();

            var recordsFoundSelectionLocation = By.XPath(String.Format(matchedRecordResultPartialLocation, currentRequest.Name.Split(' ').Last()));

            wait.Until(D => D.FindElements(searchErrorLocation).Any() || D.FindElements(recordsFoundSelectionLocation).Count.Equals(1));

            var recordWasFound = driver.FindElements(recordsFoundSelectionLocation).Count == 1;

            driver.FindElement(recordsFoundSelectionLocation).Click();
            driver.FindElement(continueMemberSelectionLocation).Click();

            return recordWasFound;
        }

        private void EnterMembershipInfo(IWebDriver driver, WebDriverWait wait, MembershipRecordInfo currentRequest)
        {
            wait.Until(ExpectedConditions.ElementIsVisible(phoneTextBoxLocation));
            driver.FindElement(phoneTextBoxLocation).Clear();
            driver.FindElement(phoneTextBoxLocation).SendKeys(currentRequest.PhoneNumber);

            driver.FindElement(emailTextBoxLocation).Clear();
            driver.FindElement(emailTextBoxLocation).SendKeys(currentRequest.EmailAddress);

            driver.FindElement(street1TextBoxLocation).Clear();
            driver.FindElement(street1TextBoxLocation).SendKeys(currentRequest.ServiceAddress1);

            driver.FindElement(street2TextBoxLocation).Clear();
            driver.FindElement(street2TextBoxLocation).SendKeys(currentRequest.ServiceAddress2);

            driver.FindElement(cityTextBoxLocation).Clear();
            driver.FindElement(cityTextBoxLocation).SendKeys("Logan");

            driver.FindElement(zipCodeTextBoxLocation).Clear();
            driver.FindElement(zipCodeTextBoxLocation).SendKeys("84321");

            driver.FindElement(moveIntoWardButtonLocation).Click();

            wait.Until(ExpectedConditions.ElementIsVisible(suggestedRadioButtonLocation));
            driver.FindElement(suggestedRadioButtonLocation).Click();

            wait.Until(ExpectedConditions.ElementToBeClickable(moveIntoWardButtonLocation));
            driver.FindElement(moveIntoWardButtonLocation).Click();

            wait.Until(D => D.PageSource.Contains(recordsMovedSuccessMessage));
        }

        public void Dispose()
        {
            KeepRequestorActive = false;
        }
    }
}
