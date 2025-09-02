import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDispatch } from 'react-redux';
import { Star, Send, ThumbsUp } from 'lucide-react';
import { submitRating } from '../store/slices/chatSlice';
import { AppDispatch } from '../store/store';

interface FeedbackMessageProps {
  conversationId: string;
  messageId: string;
  metadata?: {
    ratingScale?: {
      min: number;
      max: number;
      labels: string[];
    };
    feedbackPrompt?: string;
  };
}

export const FeedbackMessage: React.FC<FeedbackMessageProps> = ({
  conversationId,
  messageId,
  metadata,
}) => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const [rating, setRating] = useState(0);
  const [feedback, setFeedback] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const ratingScale = metadata?.ratingScale || {
    min: 1,
    max: 5,
    labels: ['Poor', 'Fair', 'Good', 'Very Good', 'Excellent']
  };

  const feedbackPrompt = metadata?.feedbackPrompt || t('feedback.defaultPrompt', 'How would you rate our service today?');

  const handleSubmit = async () => {
    if (rating === 0) return;

    setIsSubmitting(true);
    try {
      await dispatch(submitRating({
        conversationId,
        messageId,
        rating,
        feedback: feedback.trim() || undefined,
      })).unwrap();
      
      setSubmitted(true);
    } catch (error) {
      console.error('Failed to submit rating:', error);
      // You could add error handling here
    } finally {
      setIsSubmitting(false);
    }
  };

  if (submitted) {
    return (
      <div className="feedback-message submitted">
        <div className="feedback-header">
          <ThumbsUp className="feedback-icon success" size={20} />
          <span className="feedback-title">{t('feedback.thankYou', 'Thank you for your feedback!')}</span>
        </div>
        <p className="feedback-subtitle">
          {t('feedback.appreciated', 'Your feedback helps us improve our service.')}
        </p>
      </div>
    );
  }

  return (
    <div className="feedback-message">
      <div className="feedback-header">
        <span className="feedback-title">{feedbackPrompt}</span>
      </div>
      
      <div className="rating-section">
        <div className="rating-stars">
          {Array.from({ length: ratingScale.max - ratingScale.min + 1 }, (_, i) => {
            const starValue = ratingScale.min + i;
            return (
              <button
                key={starValue}
                onClick={() => setRating(starValue)}
                className={`star-button ${starValue <= rating ? 'active' : ''}`}
                title={ratingScale.labels[i] || `${starValue} stars`}
                disabled={isSubmitting}
              >
                <Star 
                  size={24} 
                  fill={starValue <= rating ? 'currentColor' : 'none'}
                  className="star-icon"
                />
              </button>
            );
          })}
        </div>
        
        {rating > 0 && (
          <span className="rating-label">
            {ratingScale.labels[rating - ratingScale.min] || `${rating} stars`}
          </span>
        )}
      </div>

      <div className="feedback-section">
        <textarea
          value={feedback}
          onChange={(e) => setFeedback(e.target.value)}
          placeholder={t('feedback.placeholder', 'Share your thoughts (optional)...')}
          className="feedback-textarea"
          disabled={isSubmitting}
          maxLength={500}
        />
      </div>

      <div className="feedback-actions">
        <button
          onClick={handleSubmit}
          disabled={rating === 0 || isSubmitting}
          className="feedback-submit-btn"
        >
          {isSubmitting ? (
            <>
              <div className="spinner" />
              {t('feedback.submitting', 'Submitting...')}
            </>
          ) : (
            <>
              <Send size={16} />
              {t('feedback.submit', 'Submit Rating')}
            </>
          )}
        </button>
      </div>
    </div>
  );
};
