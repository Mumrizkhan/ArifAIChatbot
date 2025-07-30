import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Mic, Square } from 'lucide-react';
import { voiceService } from '../services/voiceService';

interface VoiceRecorderProps {
  onRecordingComplete: (audioBlob: Blob) => void;
  onRecordingStart?: () => void;
  onRecordingStop?: () => void;
}

export const VoiceRecorder: React.FC<VoiceRecorderProps> = ({
  onRecordingComplete,
  onRecordingStart,
  onRecordingStop,
}) => {
  const { t } = useTranslation();
  const [isRecording, setIsRecording] = useState(false);
  const [recordingTime, setRecordingTime] = useState(0);

  useEffect(() => {
    let interval: NodeJS.Timeout;
    
    if (isRecording) {
      interval = setInterval(() => {
        setRecordingTime(prev => prev + 1);
      }, 1000);
    } else {
      setRecordingTime(0);
    }

    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isRecording]);

  const handleStartRecording = async () => {
    if (!voiceService.isSupported()) {
      alert(t('errors.voiceNotSupported'));
      return;
    }

    try {
      await voiceService.startRecording();
      setIsRecording(true);
      onRecordingStart?.();
    } catch (error) {
      console.error('Failed to start recording:', error);
      alert(t('errors.voiceRecordingFailed'));
    }
  };

  const handleStopRecording = async () => {
    try {
      const audioBlob = await voiceService.stopRecording();
      setIsRecording(false);
      onRecordingComplete(audioBlob);
      onRecordingStop?.();
    } catch (error) {
      console.error('Failed to stop recording:', error);
      setIsRecording(false);
      alert(t('errors.voiceRecordingFailed'));
    }
  };

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  if (isRecording) {
    return (
      <div className="voice-recorder recording">
        <div className="recording-indicator">
          <div className="recording-dot"></div>
          <span className="recording-time">{formatTime(recordingTime)}</span>
        </div>
        <button
          onClick={handleStopRecording}
          className="voice-button stop"
          aria-label={t('widget.stopRecording')}
        >
          <Square size={20} />
        </button>
      </div>
    );
  }

  return (
    <button
      onClick={handleStartRecording}
      className="voice-button start"
      aria-label={t('widget.recordVoice')}
    >
      <Mic size={20} />
    </button>
  );
};
