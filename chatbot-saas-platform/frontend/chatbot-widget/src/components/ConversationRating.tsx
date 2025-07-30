import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Star, ThumbsUp } from 'lucide-react';

interface ConversationRatingProps {
  onRatingSubmit: (rating: number, feedback?: string) => void;
}

export const ConversationRating: React.FC<ConversationRatingProps> = ({
  onRatingSubmit,
}) => {
  const { t } = useTranslation();
  const [rating, setRating] = useState(0);
  const [feedback, setFeedback] = useState('');
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = () => {
    onRatingSubmit(rating, feedback);
    setSubmitted(true);
  };

  if (submitted) {
    return (
      <div className="rating-submitted">
        <ThumbsUp size={16} />
        <span>{t('rating.thankYou')}</span>
      </div>
    );
  }

  return (
    <div className="conversation-rating">
      <div className="rating-header">
        <span>{t('rating.rateConversation')}</span>
      </div>
      
      <div className="rating-stars">
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            onClick={() => setRating(star)}
            className={`star ${star <= rating ? 'active' : ''}`}
          >
            <Star size={20} fill={star <= rating ? 'currentColor' : 'none'} />
          </button>
        ))}
      </div>

      <textarea
        value={feedback}
        onChange={(e) => setFeedback(e.target.value)}
        placeholder={t('rating.feedbackPlaceholder')}
        className="rating-feedback"
      />

      <button
        onClick={handleSubmit}
        disabled={rating === 0}
        className="rating-submit"
      >
        {t('rating.submit')}
      </button>
    </div>
  );
};
