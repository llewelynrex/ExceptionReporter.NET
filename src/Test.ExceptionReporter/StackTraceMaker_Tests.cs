using System;
using ExceptionReporting.Report;
using NUnit.Framework;

namespace ExceptionReporting.Tests
{
	public class StackTraceMaker_Tests
	{
		[Test]
		public void Can_Create_StackTrace_With_1_Exception()
		{
			var maker = new StackTraceMaker(new TestException());
			var stackTrace = maker.FullStackTrace();
			
			Assert.That(stackTrace, 
				Is.EqualTo(string.Format(
					"Top-level Exception{0}Type:    ExceptionReporting.Tests.TestException{0}Message: {1}{0}Source:{0}", 
						Environment.NewLine, TestException.ErrorMessage)));
		}
		
		[Test]
		public void Can_Create_StackTrace_With_2_Exceptions()
		{
			// both exceptions are identical, but this still serves our testing purposes
			var maker = new StackTraceMaker(new TestException(), new TestException());
			var stackTrace = maker.FullStackTrace();
			
			Assert.That(stackTrace, 
				Is.EqualTo(string.Format(
					"Top-level Exception{0}Type:    ExceptionReporting.Tests.TestException{0}Message: {1}{0}Source:{0}" + 
					"Top-level Exception{0}Type:    ExceptionReporting.Tests.TestException{0}Message: {1}{0}Source:{0}", 
						Environment.NewLine, TestException.ErrorMessage)));
		}
		
		[Test]
		public void Can_Create_StackTrace_With_1_InnerException()
		{
			var maker = new StackTraceMaker(new TestContainsInnerException());
			var stackTrace = maker.FullStackTrace();
			
			Assert.That(stackTrace, 
				Is.EqualTo(string.Format(
					"Top-level Exception{0}Type:    ExceptionReporting.Tests.TestContainsInnerException{0}Message: {1}{0}Source:  {0}" + 
					"Inner Exception 1{0}Type:    ExceptionReporting.Tests.TestInnerException{0}Message: {2}{0}Source:{0}", 
					Environment.NewLine, TestContainsInnerException.ErrorMessage, TestInnerException.ErrorMessage)));
		}
	}
}