using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace LinkedIn_Scraper
{
	internal class Program
	{
		private const string loadJQueryScript = @"  /** dynamically load jQuery */
													(function(jqueryUrl, callback) {
														if (typeof jqueryUrl != 'string') {
															jqueryUrl = 'https://ajax.googleapis.com/ajax/libs/jquery/1.7.2/jquery.min.js';
														}
														if (typeof jQuery == 'undefined') {
															var script = document.createElement('script');
															var head = document.getElementsByTagName('head')[0];
															var done = false;
															script.onload = script.onreadystatechange = (function() {
																if (!done && (!this.readyState || this.readyState == 'loaded' 
																		|| this.readyState == 'complete')) {
																	done = true;
																	script.onload = script.onreadystatechange = null;
																	head.removeChild(script);
																	callback();
																}
															});
															script.src = jqueryUrl;
															head.appendChild(script);
														}
														else {
															callback();
														}
													})(arguments[0], arguments[arguments.length - 1]);";

		private const string expandAllScript = @"   //expand all, new design
													function expandAll(callback) {
														window.scrollTo(0, document.body.scrollHeight);
														//wait for page to render after scroll
														setTimeout(function() {
															var count = 0;
															//try to expand everything a lot of times
															var interval = setInterval(function() {
																var $button = $('button[aria-controls=""languages-expandable-content""]');
																if ($button.length === 1 && $button.attr('aria-expanded') === 'false') {
																	$button.get(0).click();
																}

																$button = $('.lt-line-clamp__more');
																
																	$button.click();
																

																$button = $('button[data-control-name=""skill_details""]');
																if ($button.length === 1 && $button.attr('aria-expanded') === 'false') {
																	$button.get(0).click();
																}

																$button = $('button[data-control-name=""accomplishments_expand_languages""]');
																if ($button.length === 1) {
																	$button.get(0).click();
																}

																$('.experience-section button.pv-profile-section__see-more-inline').each(function() {
																	$button = $(this);
																	if ($button.attr('aria-expanded') === 'false') {
																		$button.get(0).click();
																	}
																});

																count++;
																if (count > 25) {
																	clearInterval(interval);
																	window.scrollTo(0, 0);
																	//wait until expanded & then execute callback
																	callback();
																}
															}, 25);
														}, 50);
													}

													expandAll(arguments[0]);";

		private const string expandMainScript = @"  //expand contact & personal info, new design
													function expandMain(callback) {
														var count = 0;
														var interval = setInterval(function() {
															var $button = $('button[data-control-name=""contact_see_more""]');
															if ($button.length === 1) {
																$button.get(0).click();
															}

															count++;
															if (count > 10) {
																clearInterval(interval);
																//wait until expanded & then execute callback
																callback();
															}
														}, 5);
													}

													expandMain(arguments[0]);";

		private const string getDataScript = @" //helpers
												$.fn.textSeparated = function() {
													return $(this).map(function() {
														return $(this).text();
													})
													.toArray()
													.join(', ');
												}

												var data = {};
												data.linkedin = true;

												function getData(callback) {
													//get data from html
													var phone = $('.pv-contact-info__contact-type.ci-phone .pv-contact-info__ci-container:first-of-type').text().trim();
													if (phone.lastIndexOf(')') === phone.length - 1) {
														phone = phone.substring(0, phone.lastIndexOf('(')).trim();
													}
													data.email = $('.pv-contact-info__contact-type.ci-email .pv-contact-info__ci-container:first-of-type').text().trim();
													data.fullName = $('.pv-top-card-section__name').text();
													data.position = $('.pv-top-card-section__headline').text();
													data.address = $('.pv-top-card-section__location').text();
													data.industry = '';//todo: maybe fix
													data.currentEmployer = '';//todo: maybe fix
													data.previousEmployer = $('.pv-top-card-section__experience .pv-top-card-section__company').textSeparated().trim();
													data.education = $('.pv-entity__degree-info .pv-entity__school-name').textSeparated().trim();
													data.url = $('.pv-contact-info__contact-type.ci-vanity-url .pv-contact-info__ci-container:first-of-type').text().trim();
													data.phone = phone;
													data.country = $('.pv-contact-info__contact-type.ci-address .pv-contact-info__ci-container a').text().trim();
													data.pictureUrl = $('.pv-top-card-section__photo img:not(.ghost-person)').attr('src');
													if (!data.pictureUrl) {
														data.pictureUrl = $('.presence-entity__image').css('background-image')
														if(!!data.pictureUrl){
															data.pictureUrl = data.pictureUrl.replace('url(""', '').replace('"")', '')
														}
													}
													data.summary = $('.pv-top-card-section__summary .pv-top-card-section__summary-text').text().trim();
													data.expirience = [];
													$('.experience-section .pv-profile-section__section-info li.pv-profile-section__card-item-v2').each(function() {
														var item = {};
														item.company = $(this).find('.pv-entity__summary-info h4 .pv-entity__secondary-title').text();
														item.position = $(this).find('.pv-entity__summary-info h3').text();
														item.location = $(this).find('.pv-entity__summary-info .pv-entity__location span:not(.visually-hidden)').text();
														item.time = $(this).find('.pv-entity__summary-info .pv-entity__date-range span:not(.visually-hidden)').text();
														item.description = $(this).find('.pv-entity__extra-details p').text().trim();
														data.expirience.push(item);
													});
													data.languages = [];
													$('.pv-accomplishments-block.languages .pv-accomplishments-block__list li').each(function() {
														var item = {};
														item.language = $(this).find('h4').text().replace($(this).find('h4 span.visually-hidden').text(), '').trim();
														item.proficiency = $(this).find('.pv-accomplishment-entity__proficiency').text().trim();
														data.languages.push(item);
													});
													data.skills = [];
													$('.pv-skill-categories-section__top-skills li .pv-skill-category-entity__skill-wrapper').each(function() {
														var item = {};
														item.name = $(this).find('.pv-skill-category-entity__name ').text();
														item.level = $(this).find('.pv-skill-category-entity__endorsement-count').text() || '0';
														item.level = item.level.replace('+', '');
														data.skills.push(item);
													});
													$('.pv-profile-section__section-info .pv-skill-category-list__skills_list li').each(function() {
														var item = {};
														item.name = $(this).find('.pv-skill-category-entity__name ').text();
														item.level = $(this).find('.pv-skill-category-entity__endorsement-count').text() || '0';
														item.level = item.level.replace('+', '');
														data.skills.push(item);
													});
													data.institutions = [];
													$('.education-section .pv-profile-section__section-info li').each(function() {
														var item = {};
														item.institution = $(this).find('.pv-entity__school-name').text();
														item.position = $(this).find('.pv-entity__degree-name .pv-entity__comma-item, .pv-entity__fos .pv-entity__comma-item').textSeparated();
														item.time = $(this).find('.pv-entity__dates span:not(.visually-hidden)').text();
														data.institutions.push(item);
													});

													//execute callback
													callback(data);
												}

												getData(arguments[0]);";

		private static int index = 1;

		private static void TakeScreenshot(IWebDriver driver)
		{
			string screenshotPath = $"D:/screenshot_{index++}.png";
			((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(screenshotPath, ScreenshotImageFormat.Png);
		}

		private static void Main(string[] args)
		{
			Console.OutputEncoding = Encoding.UTF8;

			ChromeOptions options = new ChromeOptions();
			//options.AddArgument("--headless");
			//options.AddArgument("--disable-gpu");
			options.AddArgument("--window-size=1600,900");
			options.AddArgument(@"user-data-dir=C:\Chrome-Profile");

			IWebDriver driver = new ChromeDriver(options);
			IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;

			driver.Navigate().GoToUrl("https://www.linkedin.com/in/ren%C3%A9-secher-3754003a/");

			//TakeScreenshot(driver);
			
			//Thread.Sleep(10000);

			const string email = "jh@madeeasy.dk";
			const string password = "5Slinger";

			IWebElement emailInput = null;

			try
			{
				emailInput = driver.FindElement(By.Id("login-email"));
			}
			catch (NoSuchElementException)
			{
				//element not found
			}

			if (emailInput != null)
			{
				emailInput.SendKeys(email);

				IWebElement passwordInput = driver.FindElement(By.Id("login-password"));
				passwordInput.SendKeys(password);

				//TakeScreenshot(driver);

				passwordInput.Submit();
				//Thread.Sleep(1000);

				//TakeScreenshot(driver);

				driver.Navigate().GoToUrl("https://www.linkedin.com/in/abildskov/");
			}

			//Thread.Sleep(1000);

			//TakeScreenshot(driver);

			for (int i = 1; i <= 5; i++)
			{
				jsExecutor.ExecuteScript($"window.scrollTo(0, {i * 900});");
				Thread.Sleep(100);
			}

			jsExecutor.ExecuteAsyncScript(loadJQueryScript);
			jsExecutor.ExecuteAsyncScript(expandMainScript);
			jsExecutor.ExecuteAsyncScript(expandAllScript);

			//TakeScreenshot(driver);

			Dictionary<string, object> dictionary = (Dictionary<string, object>)jsExecutor.ExecuteAsyncScript(getDataScript);
			Console.WriteLine("----------------------------------------------------------------------------");
			Console.WriteLine(JsonConvert.SerializeObject(dictionary, Formatting.Indented));
			Console.WriteLine("----------------------------------------------------------------------------");

			//File.WriteAllText(@"D:/json_1.json", JsonConvert.SerializeObject(dictionary, Formatting.Indented));

			//driver.Quit();
			//Thread.Sleep(100000);

			//System.Diagnostics.Process.Start("D:/screenshot_1.png");
		}
	}
}