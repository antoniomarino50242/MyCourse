using System;
using System.Collections.Generic;
using MyCourse.Models.Enums;
using MyCourse.Models.ValueTypes;

namespace MyCourse.Models.Entities
{
    public partial class Course
    {
        public Course(string title, string author, string authorId)
        {
            ChangeTitle(title);
            ChangeAuthor(author, authorId);
            changeStatus(CourseStatus.Draft);
            Lessons = new HashSet<Lesson>();
            CurrentPrice = new Money(Currency.EUR, 0);
            FullPrice = new Money(Currency.EUR, 0);
            ImagePath = "/Courses/default.png";
        }

        

        public int Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string Author { get;  set; }
        public string Email { get; private set; }
        public double Rating { get; private set; }
        public Money FullPrice { get; private set; }
        public Money CurrentPrice { get; private set; }
        public virtual ICollection<Lesson> Lessons { get; private set; }
        public string RowVersion { get; private set; }
        public CourseStatus Status { get; private set; }
        public string AuthorId { get; set; }
        public virtual ApplicationUser AuthorUser {get; set; }

        public void changeStatus(CourseStatus newStatus)
        {
            //Aggiungere logica di validazione(Come cambiare stato solo se ci stà la descrizione del corso) 
            Status = newStatus;
        }

        public void ChangeTitle(string newTitle) {
            if (string.IsNullOrWhiteSpace(newTitle))
            {
                throw new ArgumentException("The course must have a title");
            }
            Title = newTitle;
        }

        public void ChangePrice(Money newFullPrice, Money newDiscountPrice){
            if (newFullPrice == null || newDiscountPrice == null)
            {
               throw new ArgumentException("The price can't be null."); 
            }
            if (newFullPrice.Currency != newDiscountPrice.Currency)
            {
                throw new ArgumentException("The Currency don't match");
            }
            if (newFullPrice.Amount <= newDiscountPrice.Amount)
            {
                throw new ArgumentException("Full price can't be less than the current price");
            }
            FullPrice = newFullPrice;
            CurrentPrice = newDiscountPrice;
        }

        public void ChangeDescription(string newDescription)
        {
            if (string.IsNullOrEmpty(newDescription))
            {
                throw new ArgumentException("The Description can't be null.");
            }
            Description = newDescription;
        }

        public void ChangeEmail(string newEmail)
        {
            if (string.IsNullOrEmpty(newEmail))
            {
                throw new ArgumentException("The Email can't be empty");
            }
            Email = newEmail;
        }

        public void ChangeImagePath(string newImagePath)
        {
            if (string.IsNullOrEmpty(newImagePath))
            {
                throw new ArgumentException("The image path can't be empty");
            }
            ImagePath = newImagePath;
        }
        private void ChangeAuthor(string newAuthor, string newAuthorId)
        {
            if (string.IsNullOrWhiteSpace(newAuthor))
            {
                throw new ArgumentException("The Author must have a name");
            }
            if (string.IsNullOrWhiteSpace(newAuthorId))
            {
                throw new ArgumentException("The Author must have an id");
            }
            Author = newAuthor;
            AuthorId = newAuthorId;
        }
    }
}
