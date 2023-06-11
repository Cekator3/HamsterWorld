namespace HamsterWorld.Models
{
   public class AddNewFeedbackBindingModel
   {
      public const ushort FEEDBACK_MAX_RATING = 10;
      public const ushort FEEDBACK_MIN_RATING = 1;
      private ushort _feedbackRating;
      public int ProductId { get; set; }
      public string FeedbackText { get; set; } = "";
      public ushort FeedbackRating 
      { 
         get
         {
            return _feedbackRating;
         }
         set
         {
            if(value < FEEDBACK_MIN_RATING)
            {
               value = FEEDBACK_MIN_RATING;
            }
            if(value > FEEDBACK_MAX_RATING)
            {
               value = FEEDBACK_MAX_RATING;
            }

            _feedbackRating = value;
         }
      }
   }
}