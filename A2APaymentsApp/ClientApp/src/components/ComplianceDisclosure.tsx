import React from 'react';
import XUIButton from '@xero/xui/react/button';
import XUIModal from '@xero/xui/react/modal';
import type { ComplianceVariant } from '../types/EntryContext';
import './ComplianceDisclosure.css';

interface ComplianceDisclosureProps {
  variant: ComplianceVariant;
  isOpen: boolean;
  onAcknowledge: () => void;
  onCancel?: () => void;
}

export const ComplianceDisclosure: React.FC<ComplianceDisclosureProps> = ({
  variant,
  isOpen,
  onAcknowledge,
  onCancel,
}) => {
  if (variant === 'modal') {
    return (
      <XUIModal
        isOpen={isOpen}
        onClose={onCancel || onAcknowledge}
        closeButtonLabel="Close"
        size="medium"
      >
        <div className="compliance-disclosure-modal">
          <div className="compliance-modal-header">
            <h2 className="xui-heading-2">Before you continue</h2>
          </div>
          
          <div className="compliance-modal-content">
            <p className="xui-font-size-4" style={{ marginBottom: '16px' }}>
              You're about to set up <strong>Pay by bank (powered by Akahu)</strong> as a payment method for your customers.
            </p>
            
            <div className="compliance-info-box">
              <h3 className="xui-heading-6" style={{ marginBottom: '8px' }}>
                How it works
              </h3>
              <p className="xui-font-size-5" style={{ marginBottom: '12px' }}>
                When your customers pay by bank, funds are transferred directly from their bank account to yours. Neither Xero nor Akahu hold or custody any funds during this process.
              </p>
              <p className="xui-font-size-5" style={{ marginBottom: '0' }}>
                Akahu provides the secure connection that enables these direct bank-to-bank transfers.
              </p>
            </div>
            
            <div className="compliance-links">
              <a href="#" className="xui-link">
                Learn more about Pay by bank
              </a>
              {' · '}
              <a href="#" className="xui-link">
                Akahu terms and conditions
              </a>
            </div>
          </div>
          
          <div className="compliance-modal-actions">
            {onCancel && (
              <XUIButton
                variant="borderless-main"
                onClick={onCancel}
              >
                Go back
              </XUIButton>
            )}
            <XUIButton
              variant="main"
              onClick={onAcknowledge}
            >
              I understand, continue
            </XUIButton>
          </div>
        </div>
      </XUIModal>
    );
  }

  if (variant === 'banner') {
    return (
      <div className="compliance-disclosure-banner">
        <div className="compliance-banner-content">
          <div className="compliance-banner-icon">ℹ️</div>
          <div className="compliance-banner-text">
            <p className="xui-font-size-5">
              <strong>Pay by bank (powered by Akahu)</strong> enables direct bank-to-bank transfers. 
              Neither Xero nor Akahu hold customer funds.{' '}
              <a href="#" className="xui-link">Learn more</a>
            </p>
          </div>
        </div>
      </div>
    );
  }

  if (variant === 'fullscreen') {
    return (
      <div className="compliance-disclosure-fullscreen">
        <div className="compliance-fullscreen-container">
          <div className="compliance-fullscreen-header">
            <h1 className="xui-heading-1">Understanding Pay by bank</h1>
          </div>
          
          <div className="compliance-fullscreen-content">
            <section className="compliance-section">
              <h2 className="xui-heading-3">What is Pay by bank?</h2>
              <p className="xui-font-size-4">
                Pay by bank (powered by Akahu) is a payment method that enables your customers 
                to pay invoices directly from their bank account to yours.
              </p>
            </section>
            
            <section className="compliance-section">
              <h2 className="xui-heading-3">How it works</h2>
              <p className="xui-font-size-4">
                When a customer chooses to pay by bank, Akahu provides a secure connection 
                that facilitates a direct transfer between their bank and yours. This is a 
                direct bank-to-bank transfer – neither Xero nor Akahu hold, custody, or 
                process funds at any point.
              </p>
            </section>
            
            <section className="compliance-section">
              <h2 className="xui-heading-3">Important information</h2>
              <ul className="compliance-list">
                <li className="xui-font-size-4">
                  Funds are transferred directly between bank accounts
                </li>
                <li className="xui-font-size-4">
                  Xero and Akahu do not hold or custody any customer funds
                </li>
                <li className="xui-font-size-4">
                  Settlement typically occurs within 1-2 business days
                </li>
                <li className="xui-font-size-4">
                  You must comply with Akahu's terms and conditions
                </li>
              </ul>
            </section>
            
            <div className="compliance-fullscreen-checkbox">
              <label className="compliance-checkbox-label">
                <input type="checkbox" required />
                <span className="xui-font-size-4">
                  I understand that Pay by bank facilitates direct bank transfers and that 
                  neither Xero nor Akahu hold customer funds
                </span>
              </label>
            </div>
            
            <div className="compliance-fullscreen-links">
              <a href="#" className="xui-link">Learn more about Pay by bank</a>
              {' · '}
              <a href="#" className="xui-link">Akahu terms and conditions</a>
              {' · '}
              <a href="#" className="xui-link">Privacy policy</a>
            </div>
          </div>
          
          <div className="compliance-fullscreen-actions">
            {onCancel && (
              <XUIButton
                variant="borderless-main"
                onClick={onCancel}
              >
                Go back
              </XUIButton>
            )}
            <XUIButton
              variant="main"
              onClick={onAcknowledge}
            >
              Continue to setup
            </XUIButton>
          </div>
        </div>
      </div>
    );
  }

  return null;
};
